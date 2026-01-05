using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples.Server;

/// <summary>
///     Server-side ripple implementation with direct Orleans grain access.
/// </summary>
/// <typeparam name="TProjection">The projection state type.</typeparam>
/// <remarks>
///     <para>
///         <see cref="ServerRipple{TProjection}" /> provides direct access to Orleans grains
///         without serialization overhead. It is designed for Blazor Server scenarios where
///         the application runs in the same process as Orleans.
///     </para>
/// </remarks>
internal sealed class ServerRipple<TProjection> : IRipple<TProjection>
    where TProjection : class
{
    private readonly SemaphoreSlim subscriptionLock = new(1, 1);

    private string? currentEntityId;

    private bool isDisposed;

    private IDisposable? signalRSubscription;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ServerRipple{TProjection}" /> class.
    /// </summary>
    /// <param name="projectionGrainFactory">Factory for resolving UX projection grains.</param>
    /// <param name="signalRNotifier">SignalR notifier for real-time updates.</param>
    /// <param name="logger">Logger instance.</param>
    public ServerRipple(
        IUxProjectionGrainFactory projectionGrainFactory,
        IProjectionUpdateNotifier signalRNotifier,
        ILogger<ServerRipple<TProjection>> logger
    )
    {
        ArgumentNullException.ThrowIfNull(projectionGrainFactory);
        ArgumentNullException.ThrowIfNull(signalRNotifier);
        ArgumentNullException.ThrowIfNull(logger);
        ProjectionGrainFactory = projectionGrainFactory;
        SignalRNotifier = signalRNotifier;
        Logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public event EventHandler<RippleErrorEventArgs>? ErrorOccurred;

    /// <inheritdoc />
    public TProjection? Current { get; private set; }

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    /// <inheritdoc />
    public bool IsLoaded { get; private set; }

    /// <inheritdoc />
    public bool IsLoading { get; private set; }

    /// <inheritdoc />
    public Exception? LastError { get; private set; }

    /// <inheritdoc />
    public long? Version { get; private set; }

    private ILogger<ServerRipple<TProjection>> Logger { get; }

    private IUxProjectionGrainFactory ProjectionGrainFactory { get; }

    private IProjectionUpdateNotifier SignalRNotifier { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (isDisposed)
        {
            return;
        }

        await subscriptionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            signalRSubscription?.Dispose();
            signalRSubscription = null;
            Current = null;
            currentEntityId = null;
            IsLoaded = false;
            IsConnected = false;
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
        await subscriptionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (isDisposed)
            {
                return;
            }

            // Re-capture after acquiring lock - may have been set to null by another thread
            string? entityId = currentEntityId;
            if (string.IsNullOrEmpty(entityId))
            {
                return;
            }

            IsLoading = true;
            OnChanged();
            try
            {
                IUxProjectionGrain<TProjection> grain =
                    ProjectionGrainFactory.GetUxProjectionGrain<TProjection>(entityId);
                TProjection? projection = await grain.GetAsync(cancellationToken).ConfigureAwait(false);
                BrookPosition version = await grain.GetLatestVersionAsync(cancellationToken).ConfigureAwait(false);
                Current = projection;
                Version = version.NotSet ? null : version.Value;
                IsLoading = false;
                Logger.ProjectionRefreshed(typeof(TProjection).Name, entityId, Version);
                OnChanged();
            }
            catch (Exception ex)
            {
                LastError = ex;
                IsLoading = false;
                Logger.ProjectionRefreshFailed(typeof(TProjection).Name, entityId, ex);
                OnErrorOccurred(ex);
                OnChanged();
                throw;
            }
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
        await subscriptionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            // Unsubscribe from previous entity if any
            if (currentEntityId != null)
            {
                UnsubscribeCore();
            }

            currentEntityId = entityId;
            IsLoading = true;
            LastError = null;
            OnChanged();
            try
            {
                // Direct grain access - no serialization overhead
                IUxProjectionGrain<TProjection> grain =
                    ProjectionGrainFactory.GetUxProjectionGrain<TProjection>(entityId);
                TProjection? projection = await grain.GetAsync(cancellationToken).ConfigureAwait(false);
                BrookPosition version = await grain.GetLatestVersionAsync(cancellationToken).ConfigureAwait(false);
                Current = projection;
                Version = version.NotSet ? null : version.Value;
                IsLoaded = true;
                IsLoading = false;
                IsConnected = true;
                Logger.ProjectionSubscribed(typeof(TProjection).Name, entityId, Version);

                // Dispose previous before re-assigning
                signalRSubscription?.Dispose();

                // Subscribe to SignalR notifications for updates
                signalRSubscription = SignalRNotifier.Subscribe(
                    typeof(TProjection).Name,
                    entityId,
                    OnProjectionUpdated);
                OnChanged();
            }
            catch (Exception ex)
            {
                LastError = ex;
                IsLoading = false;
                Logger.ProjectionSubscriptionFailed(typeof(TProjection).Name, entityId, ex);
                OnErrorOccurred(ex);
                OnChanged();
                throw;
            }
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
        await subscriptionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            UnsubscribeCore();
        }
        finally
        {
            subscriptionLock.Release();
        }
    }

    private void OnChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnErrorOccurred(
        Exception exception
    )
    {
        ErrorOccurred?.Invoke(this, new(exception));
    }

    private void OnProjectionUpdated(
        ProjectionUpdatedEvent args
    )
    {
        // Fire and forget refresh - we don't want to block the SignalR callback
        _ = RefreshAsync(CancellationToken.None);
    }

    private void UnsubscribeCore()
    {
        signalRSubscription?.Dispose();
        signalRSubscription = null;
        if (currentEntityId != null)
        {
            Logger.ProjectionUnsubscribed(typeof(TProjection).Name, currentEntityId);
        }

        Current = null;
        currentEntityId = null;
        Version = null;
        IsLoaded = false;
        IsConnected = false;
        OnChanged();
    }
}