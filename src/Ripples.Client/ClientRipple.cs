using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples.Client;

/// <summary>
///     Client-side implementation of <see cref="IRipple{TProjection}" /> for Blazor WebAssembly.
/// </summary>
/// <typeparam name="TProjection">The type of projection.</typeparam>
/// <remarks>
///     <para>
///         This implementation uses HTTP to fetch projection data and SignalR
///         to receive real-time updates. ETags are used for efficient cache validation.
///     </para>
/// </remarks>
internal sealed class ClientRipple<TProjection> : IRipple<TProjection>
    where TProjection : class
{
    private readonly SemaphoreSlim subscriptionLock = new(1, 1);

    private string? currentETag;

    private bool isDisposed;

    private IDisposable? signalRSubscription;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ClientRipple{TProjection}" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API requests.</param>
    /// <param name="signalRConnection">The SignalR connection for updates.</param>
    /// <param name="routeProvider">The route provider for URL construction.</param>
    /// <param name="logger">The logger.</param>
    public ClientRipple(
        HttpClient httpClient,
        ISignalRRippleConnection signalRConnection,
        IProjectionRouteProvider routeProvider,
        ILogger<ClientRipple<TProjection>> logger
    )
    {
        HttpClient = httpClient;
        SignalRConnection = signalRConnection;
        RouteProvider = routeProvider;
        Logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public event EventHandler<RippleErrorEventArgs>? ErrorOccurred;

    /// <inheritdoc />
    public TProjection? Current { get; private set; }

    /// <summary>
    ///     Gets the currently subscribed entity identifier.
    /// </summary>
    public string? EntityId { get; private set; }

    /// <inheritdoc />
    public bool IsConnected => SignalRConnection.State == SignalRConnectionState.Connected;

    /// <inheritdoc />
    public bool IsLoaded { get; private set; }

    /// <inheritdoc />
    public bool IsLoading { get; private set; }

    /// <inheritdoc />
    public Exception? LastError { get; private set; }

    /// <inheritdoc />
    public long? Version { get; private set; }

    private HttpClient HttpClient { get; }

    private ILogger<ClientRipple<TProjection>> Logger { get; }

    private IProjectionRouteProvider RouteProvider { get; }

    private ISignalRRippleConnection SignalRConnection { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        await subscriptionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await UnsubscribeInternalAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            subscriptionLock.Release();
            subscriptionLock.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task RefreshAsync(
        CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        if (string.IsNullOrEmpty(EntityId))
        {
            return;
        }

        await subscriptionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            IsLoading = true;
            LastError = null;
            RaiseChanged();

            // Force refresh by clearing ETag
            currentETag = null;
            await FetchProjectionAsync(EntityId, cancellationToken).ConfigureAwait(false);
            ClientRippleLoggerExtensions.RefreshedProjection(Logger, typeof(TProjection).Name, EntityId);
        }
        catch (Exception ex)
        {
            LastError = ex;
            IsLoading = false;
            RaiseError(ex);
            throw;
        }
        finally
        {
            subscriptionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SubscribeAsync(
        string entityId,
        CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        await subscriptionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Unsubscribe from previous entity if different
            if ((EntityId != null) && (EntityId != entityId))
            {
                await UnsubscribeInternalAsync(CancellationToken.None).ConfigureAwait(false);
            }

            EntityId = entityId;
            IsLoading = true;
            LastError = null;
            RaiseChanged();
            string projectionTypeName = typeof(TProjection).Name;

            // Ensure SignalR is connected
            if (SignalRConnection.State != SignalRConnectionState.Connected)
            {
                await SignalRConnection.StartAsync(cancellationToken).ConfigureAwait(false);
            }

            // Subscribe to SignalR updates
            signalRSubscription?.Dispose();
            signalRSubscription = await SignalRConnection.SubscribeAsync(
                    projectionTypeName,
                    entityId,
                    OnVersionUpdated,
                    cancellationToken)
                .ConfigureAwait(false);

            // Fetch initial data via HTTP
            await FetchProjectionAsync(entityId, cancellationToken).ConfigureAwait(false);
            ClientRippleLoggerExtensions.SubscribedToProjection(Logger, projectionTypeName, entityId);
        }
        catch (Exception ex)
        {
            LastError = ex;
            IsLoading = false;
            RaiseError(ex);
            ClientRippleLoggerExtensions.SubscriptionFailed(Logger, typeof(TProjection).Name, entityId, ex);
            throw;
        }
        finally
        {
            subscriptionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UnsubscribeAsync(
        CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        await subscriptionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await UnsubscribeInternalAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            subscriptionLock.Release();
        }
    }

    private async Task FetchProjectionAsync(
        string entityId,
        CancellationToken cancellationToken
    )
    {
        string route = RouteProvider.GetRoute<TProjection>();
        string url = $"{route}/{entityId}";
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(currentETag))
        {
            request.Headers.IfNoneMatch.Add(new(currentETag));
        }

        using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            // Data hasn't changed, keep current values
            IsLoading = false;
            RaiseChanged();
            return;
        }

        response.EnsureSuccessStatusCode();
        TProjection? projection = await response.Content.ReadFromJsonAsync<TProjection>(cancellationToken)
            .ConfigureAwait(false);
        Current = projection;
        currentETag = response.Headers.ETag?.Tag;

        // Try to extract version from response headers
        if (response.Headers.TryGetValues("X-Projection-Version", out IEnumerable<string>? versions))
        {
            foreach (string versionStr in versions)
            {
                if (long.TryParse(versionStr, out long parsedVersion))
                {
                    Version = parsedVersion;
                    break;
                }
            }
        }

        IsLoading = false;
        IsLoaded = true;
        RaiseChanged();
    }

    private void OnVersionUpdated(
        long newVersion
    )
    {
        if ((newVersion > (Version ?? -1)) && (EntityId != null))
        {
            ClientRippleLoggerExtensions.ReceivedVersionUpdate(Logger, typeof(TProjection).Name, EntityId, newVersion);

            // Trigger a refresh when a newer version is available
            _ = RefreshAsync(CancellationToken.None);
        }
    }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

    private void RaiseError(
        Exception ex
    )
    {
        RaiseChanged();
        ErrorOccurred?.Invoke(this, new(ex));
    }

    private async Task UnsubscribeInternalAsync(
        CancellationToken cancellationToken
    )
    {
        if (EntityId == null)
        {
            return;
        }

        signalRSubscription?.Dispose();
        signalRSubscription = null;
        await SignalRConnection.UnsubscribeAsync(typeof(TProjection).Name, EntityId, cancellationToken)
            .ConfigureAwait(false);
        ClientRippleLoggerExtensions.UnsubscribedFromProjection(Logger, typeof(TProjection).Name, EntityId);
        EntityId = null;
        Current = null;
        Version = null;
        IsLoaded = false;
        currentETag = null;
        RaiseChanged();
    }
}