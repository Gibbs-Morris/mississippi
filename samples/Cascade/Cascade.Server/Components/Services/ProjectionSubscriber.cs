// <copyright file="ProjectionSubscriber.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;


namespace Cascade.Server.Components.Services;

/// <summary>
///     Default implementation of <see cref="IProjectionSubscriber{T}" /> using SignalR and HTTP.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
/// <remarks>
///     <para>
///         This service is designed to be scoped per Blazor circuit (one per browser tab).
///         It manages its own SignalR connection and handles reconnection automatically.
///     </para>
///     <para>
///         The service uses HTTP ETags to efficiently fetch projection data only when
///         it has actually changed, reducing unnecessary network traffic.
///     </para>
/// </remarks>
internal sealed class ProjectionSubscriber<T> : IProjectionSubscriber<T>
    where T : class
{
    private const string HttpClientName = "CascadeProjections";

    private static readonly string ProjectionTypeName = typeof(T).Name;

    private readonly IHttpClientFactory httpClientFactory;

    private readonly HubConnection hubConnection;

    private readonly ILogger<ProjectionSubscriber<T>> logger;

    private readonly IDisposable projectionChangedRegistration;

    private readonly SemaphoreSlim refreshLock = new(1, 1);

    private string? currentETag;

    private string? currentEntityId;

    private string? currentSubscriptionId;

    private long? currentVersion;

    private bool disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionSubscriber{T}" /> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory for creating clients.</param>
    /// <param name="hubConnection">The SignalR hub connection.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public ProjectionSubscriber(
        IHttpClientFactory httpClientFactory,
        HubConnection hubConnection,
        ILogger<ProjectionSubscriber<T>> logger
    )
    {
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        projectionChangedRegistration = hubConnection.On<string, string, long>(
            "OnProjectionChangedAsync",
            HandleProjectionChangedAsync);
        hubConnection.Reconnected += HandleReconnectedAsync;
    }

    /// <inheritdoc />
    public event EventHandler? OnChanged;

    /// <inheritdoc />
    public event EventHandler<ProjectionErrorEventArgs>? OnError;

    /// <inheritdoc />
    public T? Current { get; private set; }

    /// <inheritdoc />
    public bool IsConnected => hubConnection.State == HubConnectionState.Connected;

    /// <inheritdoc />
    public bool IsLoaded => Current is not null;

    /// <inheritdoc />
    public bool IsLoading { get; private set; }

    /// <inheritdoc />
    public Exception? LastError { get; private set; }

    /// <inheritdoc />
    public long? Version => currentVersion;

    private static string GetBrookType()
    {
        // Look for the BrookTypeAttribute on the projection type
        // For now, we'll use a convention: {ProjectionName} -> {EntityName}Brook
        // This can be enhanced to read from attributes
        string projectionName = ProjectionTypeName;
        if (projectionName.EndsWith("Projection", StringComparison.Ordinal))
        {
            return projectionName[..^10] + "Brook";
        }

        return projectionName + "Brook";
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        // Dispose the registration
        projectionChangedRegistration.Dispose();

        // Unsubscribe from SignalR if we have an active subscription
        if (!string.IsNullOrEmpty(currentSubscriptionId) &&
            !string.IsNullOrEmpty(currentEntityId) &&
            (hubConnection.State == HubConnectionState.Connected))
        {
            try
            {
                await hubConnection.InvokeAsync(
                    "UnsubscribeFromProjectionAsync",
                    currentSubscriptionId,
                    ProjectionTypeName,
                    currentEntityId);
            }
            catch (InvalidOperationException ex)
            {
                logger.FailedToUnsubscribe(ex, ProjectionTypeName, currentEntityId);
            }
            catch (HubException ex)
            {
                logger.FailedToUnsubscribe(ex, ProjectionTypeName, currentEntityId);
            }
        }

        refreshLock.Dispose();
    }

    /// <inheritdoc />
    public async Task RefreshAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(currentEntityId))
        {
            return;
        }

        ObjectDisposedException.ThrowIf(disposed, this);

        // Use lock to prevent concurrent refreshes
        if (!await refreshLock.WaitAsync(TimeSpan.Zero, cancellationToken))
        {
            logger.RefreshAlreadyInProgress(ProjectionTypeName, currentEntityId);
            return;
        }

        try
        {
            IsLoading = true;

            // Build request with ETag for conditional GET
            string requestUrl = $"/api/projections/{ProjectionTypeName}/{currentEntityId}";
            using HttpRequestMessage request = new(HttpMethod.Get, requestUrl);
            if (!string.IsNullOrEmpty(currentETag))
            {
                request.Headers.IfNoneMatch.Add(new(currentETag));
            }

            logger.FetchingProjection(ProjectionTypeName, currentEntityId, currentETag ?? "(none)");
            using HttpClient httpClient = httpClientFactory.CreateClient(HttpClientName);
            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                logger.ProjectionNotModified(ProjectionTypeName, currentEntityId);
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.ProjectionNotFound(ProjectionTypeName, currentEntityId);
                    Current = null;
                    currentVersion = null;
                    currentETag = null;
                    OnChanged?.Invoke(this, EventArgs.Empty);
                    return;
                }

                response.EnsureSuccessStatusCode();
            }

            // Parse response
            T? newProjection = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            string? newETag = response.Headers.ETag?.Tag;

            // Extract version from ETag
            long? newVersion = null;
            if (!string.IsNullOrEmpty(newETag) && long.TryParse(newETag.Trim('"'), out long parsedVersion))
            {
                newVersion = parsedVersion;
            }

            logger.ReceivedProjectionVersion(ProjectionTypeName, currentEntityId, newVersion);

            // Update state
            Current = newProjection;
            currentETag = newETag;
            currentVersion = newVersion;
            LastError = null;
            OnChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (HttpRequestException ex)
        {
            LastError = ex;
            logger.FailedToRefresh(ex, ProjectionTypeName, currentEntityId);
            OnError?.Invoke(this, new(ex));
        }
        finally
        {
            IsLoading = false;
            refreshLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SubscribeAsync(
        string entityId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        ObjectDisposedException.ThrowIf(disposed, this);
        logger.SubscribingToProjection(ProjectionTypeName, entityId);

        // Store entity info
        currentEntityId = entityId;
        currentSubscriptionId = null;
        Current = null;
        currentVersion = null;
        currentETag = null;
        LastError = null;
        try
        {
            // Connect SignalR if needed
            await EnsureConnectedAsync(cancellationToken);

            // Subscribe via hub - get brookType from projection type attribute
            string brookType = GetBrookType();
            currentSubscriptionId = await hubConnection.InvokeAsync<string>(
                "SubscribeToProjectionAsync",
                ProjectionTypeName,
                brookType,
                entityId,
                cancellationToken);
            logger.SubscribedToProjection(currentSubscriptionId, ProjectionTypeName, entityId);

            // Initial fetch
            await RefreshAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LastError = ex;
            logger.FailedToSubscribe(ex, ProjectionTypeName, entityId);
            OnError?.Invoke(this, new(ex));
            throw;
        }
    }

    private async Task EnsureConnectedAsync(
        CancellationToken cancellationToken
    )
    {
        if (hubConnection.State == HubConnectionState.Disconnected)
        {
            logger.ConnectingToHub();
            await hubConnection.StartAsync(cancellationToken);
            logger.ConnectedToHub();
        }
    }

    private Task HandleProjectionChangedAsync(
        string projectionType,
        string entityId,
        long newVersion
    )
    {
        // Ignore notifications for other projections or entities
        if (!string.Equals(projectionType, ProjectionTypeName, StringComparison.Ordinal) ||
            !string.Equals(entityId, currentEntityId, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        // Ignore if we already have this version or newer
        if (currentVersion.HasValue && (newVersion <= currentVersion.Value))
        {
            logger.IgnoringOldVersionNotification(projectionType, entityId, newVersion, currentVersion.Value);
            return Task.CompletedTask;
        }

        logger.ReceivedChangeNotification(projectionType, entityId, newVersion);

        // Refresh in background - don't await
        _ = RefreshAsync();
        return Task.CompletedTask;
    }

    private async Task HandleReconnectedAsync(
        string? connectionId
    )
    {
        logger.SignalRReconnected(connectionId);

        // Re-subscribe if we had an active subscription
        if (!string.IsNullOrEmpty(currentEntityId))
        {
            try
            {
                string brookType = GetBrookType();
                currentSubscriptionId = await hubConnection.InvokeAsync<string>(
                    "SubscribeToProjectionAsync",
                    ProjectionTypeName,
                    brookType,
                    currentEntityId);
                logger.ResubscribedToProjection(currentSubscriptionId, ProjectionTypeName, currentEntityId);

                // Refresh to get any missed updates
                await RefreshAsync();
            }
            catch (HubException ex)
            {
                logger.FailedToResubscribe(ex, ProjectionTypeName, currentEntityId);
            }
        }
    }
}