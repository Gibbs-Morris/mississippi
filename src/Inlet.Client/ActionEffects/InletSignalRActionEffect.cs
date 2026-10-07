using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Inlet.Gateway.Abstractions;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Client.ActionEffects;

/// <summary>
///     Client-side action effect that handles projection subscription actions via SignalR.
/// </summary>
/// <remarks>
///     <para>
///         This action effect connects to the InletHub on the server and manages projection
///         subscriptions for Blazor clients. When a projection is updated on the server,
///         this action effect uses the registered <see cref="IProjectionFetcher" /> to retrieve
///         the updated data and dispatches a <see cref="ProjectionUpdatedAction{T}" />
///         to update the store.
///     </para>
///     <para>
///         This effect is scoped to <see cref="InletConnectionState" /> to provide a modular
///         compartment for SignalR connection management within the store.
///     </para>
/// </remarks>
internal sealed class InletSignalRActionEffect
    : IActionEffect<InletConnectionState>,
      IAsyncDisposable
{
    private readonly ConcurrentDictionary<(Type ProjectionType, string EntityId), SubscriptionEntry>
        activeSubscriptions = new();

    private readonly IDisposable hubCallbackRegistration;

    private readonly Dictionary<(Type ProjectionType, string EntityId), HashSet<object>> pendingSubscriptionRequests =
        new();

    private readonly object subscriptionGate = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="InletSignalRActionEffect" /> class.
    /// </summary>
    /// <param name="lazyStore">Lazy reference to the store (avoids circular dependency).</param>
    /// <param name="hubConnectionProvider">The hub connection provider.</param>
    /// <param name="projectionFetcher">The projection fetcher for retrieving projection data.</param>
    /// <param name="projectionDtoRegistry">The registry mapping DTO types to projection paths.</param>
    /// <param name="timeProvider">
    ///     The time provider for timestamps. If null, uses <see cref="TimeProvider.System" />.
    /// </param>
    public InletSignalRActionEffect(
        Lazy<IInletStore> lazyStore,
        IHubConnectionProvider hubConnectionProvider,
        IProjectionFetcher projectionFetcher,
        IProjectionDtoRegistry projectionDtoRegistry,
        TimeProvider? timeProvider = null
    )
    {
        ArgumentNullException.ThrowIfNull(lazyStore);
        ArgumentNullException.ThrowIfNull(hubConnectionProvider);
        ArgumentNullException.ThrowIfNull(projectionFetcher);
        ArgumentNullException.ThrowIfNull(projectionDtoRegistry);
        LazyStore = lazyStore;
        HubConnectionProvider = hubConnectionProvider;
        ProjectionFetcher = projectionFetcher;
        ProjectionDtoRegistry = projectionDtoRegistry;
        TimeProvider = timeProvider ?? TimeProvider.System;

        // Subscribe to projection update notifications from the server
        hubCallbackRegistration = HubConnectionProvider.RegisterHandler<string, string, long>(
            InletHubConstants.ProjectionUpdatedMethod,
            OnProjectionUpdatedAsync);

        // Handle reconnection - re-subscribe to all active subscriptions
        HubConnectionProvider.OnReconnected(OnReconnectedAsync);
    }

    private HubConnection HubConnection => HubConnectionProvider.Connection;

    private IHubConnectionProvider HubConnectionProvider { get; }

    private Lazy<IInletStore> LazyStore { get; }

    private IProjectionDtoRegistry ProjectionDtoRegistry { get; }

    private IProjectionFetcher ProjectionFetcher { get; }

    /// <summary>
    ///     Gets the store lazily to avoid circular dependency during DI resolution.
    ///     The store resolves action effects during construction, so action effects cannot depend
    ///     on the store directly in their constructor.
    /// </summary>
    private IInletStore Store => LazyStore.Value;

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public bool CanHandle(
        IAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        // Handle connection request action directly
        if (action is RequestSignalRConnectionAction)
        {
            return true;
        }

        Type actionType = action.GetType();
        if (!actionType.IsGenericType)
        {
            return false;
        }

        Type genericDef = actionType.GetGenericTypeDefinition();
        return (genericDef == typeof(SubscribeToProjectionAction<>)) ||
               (genericDef == typeof(UnsubscribeFromProjectionAction<>)) ||
               (genericDef == typeof(RefreshProjectionAction<>));
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        lock (subscriptionGate)
        {
            pendingSubscriptionRequests.Clear();
            activeSubscriptions.Clear();
        }

        hubCallbackRegistration.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        InletConnectionState currentState,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        return HandleCoreAsync(action, cancellationToken);
    }

    private async IAsyncEnumerable<IAction> HandleCoreAsync(
        IAction action,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        // Ensure connection is started (HubConnectionProvider handles state dispatching)
        await HubConnectionProvider.EnsureConnectedAsync(cancellationToken);

        // For connection request, we're done after ensuring connection
        if (action is RequestSignalRConnectionAction)
        {
            yield break;
        }

        Type actionType = action.GetType();
        Type genericDef = actionType.GetGenericTypeDefinition();
        Type projectionType = actionType.GetGenericArguments()[0];
        IInletAction inletAction = (IInletAction)action;
        string entityId = inletAction.EntityId;
        if (genericDef == typeof(SubscribeToProjectionAction<>))
        {
            await foreach (IAction resultAction in HandleSubscribeAsync(projectionType, entityId, cancellationToken))
            {
                yield return resultAction;
            }
        }
        else if (genericDef == typeof(UnsubscribeFromProjectionAction<>))
        {
            await HandleUnsubscribeAsync(projectionType, entityId, cancellationToken);
        }
        else if (genericDef == typeof(RefreshProjectionAction<>))
        {
            await foreach (IAction resultAction in HandleRefreshAsync(projectionType, entityId, cancellationToken))
            {
                yield return resultAction;
            }
        }
    }

    private sealed record SubscriptionEntry(string? SubscriptionId, object? PendingRetryToken);

#pragma warning disable CA1031 // Action effect converts exceptions to error actions instead of crashing
    private async IAsyncEnumerable<IAction> HandleRefreshAsync(
        Type projectionType,
        string entityId,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        yield return ProjectionActionFactory.CreateLoading(projectionType, entityId);
        ProjectionFetchResult? result = null;
        Exception? fetchError = null;
        bool cancelled = false;
        try
        {
            result = await ProjectionFetcher.FetchAsync(projectionType, entityId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        catch (Exception ex)
        {
            fetchError = ex;
        }

        if (cancelled)
        {
            yield break;
        }

        if (fetchError is not null)
        {
            yield return ProjectionActionFactory.CreateError(projectionType, entityId, fetchError);
            yield break;
        }

        if (result is null)
        {
            yield return ProjectionActionFactory.CreateError(
                projectionType,
                entityId,
                new InvalidOperationException($"No fetcher registered for projection type {projectionType.Name}"));
            yield break;
        }

        // NotFound (404) is valid - projection has no events yet but subscription is active.
        // Dispatch updated with null data; SignalR will push updates when events arrive.
        yield return ProjectionActionFactory.CreateUpdated(
            projectionType,
            entityId,
            result.IsNotFound ? null : result.Data,
            result.Version);
    }

    private async IAsyncEnumerable<IAction> HandleSubscribeAsync(
        Type projectionType,
        string entityId,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        (Type, string) key = (projectionType, entityId);

        // Look up the projection path from the DTO registry
        string? path = ProjectionDtoRegistry.GetPath(projectionType);
        if (path is null)
        {
            yield return ProjectionActionFactory.CreateError(
                projectionType,
                entityId,
                new InvalidOperationException($"No projection path registered for DTO type {projectionType.Name}"));
            yield break;
        }

        object request = new();
        if (!TryRegisterOrClaimFailedSubscriptionRetry(key, request, out bool isFailedSubscriptionRetry))
        {
            yield break;
        }

        string? subscriptionId = null;
        Exception? subscribeError = null;
        bool cancelled = false;
        bool isCurrentRequest = false;
        try
        {
            yield return ProjectionActionFactory.CreateLoading(projectionType, entityId);
            try
            {
                subscriptionId = await HubConnection.InvokeAsync<string>(
                    InletHubConstants.SubscribeMethod,
                    path,
                    entityId,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            catch (Exception ex)
            {
                subscribeError = ex;
            }

            if (isFailedSubscriptionRetry)
            {
                isCurrentRequest = TryCompleteFailedSubscriptionRetry(key, request, subscriptionId);
            }
            else
            {
                isCurrentRequest = TryCompletePendingSubscription(key, request, subscriptionId);
            }
        }
        finally
        {
            // Iterator disposal and failed invocations also release the pending request or retry claim.
            if (isFailedSubscriptionRetry)
            {
                _ = TryCompleteFailedSubscriptionRetry(key, request, null);
            }
            else
            {
                _ = TryCompletePendingSubscription(key, request, null);
            }
        }

        if (!isCurrentRequest)
        {
            // The owner released this request while the hub reply was pending.
            await UnsubscribeFromHubAsync(subscriptionId, path, entityId, CancellationToken.None);
            yield break;
        }

        if (cancelled)
        {
            yield break;
        }

        if (subscribeError is not null)
        {
            yield return ProjectionActionFactory.CreateError(projectionType, entityId, subscribeError);
            yield break;
        }

        // Fetch initial data
        ProjectionFetchResult? result = null;
        Exception? fetchError = null;
        cancelled = false;
        try
        {
            result = await ProjectionFetcher.FetchAsync(projectionType, entityId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        catch (Exception ex)
        {
            fetchError = ex;
        }

        if (cancelled)
        {
            yield break;
        }

        if (fetchError is not null)
        {
            yield return ProjectionActionFactory.CreateError(projectionType, entityId, fetchError);
            yield break;
        }

        if (result is null)
        {
            yield return ProjectionActionFactory.CreateError(
                projectionType,
                entityId,
                new InvalidOperationException($"No fetcher registered for projection type {projectionType.Name}"));
            yield break;
        }

        // NotFound (404) is valid - projection has no events yet but subscription is active.
        // Dispatch loaded with null data; SignalR will push updates when events arrive.
        yield return ProjectionActionFactory.CreateLoaded(
            projectionType,
            entityId,
            result.IsNotFound ? null : result.Data,
            result.Version);
    }

    private async Task HandleUnsubscribeAsync(
        Type projectionType,
        string entityId,
        CancellationToken cancellationToken
    )
    {
        (Type, string) key = (projectionType, entityId);
        SubscriptionEntry? activeSubscription;
        bool isActiveSubscriptionRemoved;
        lock (subscriptionGate)
        {
            pendingSubscriptionRequests.Remove(key);
            isActiveSubscriptionRemoved = activeSubscriptions.TryRemove(key, out activeSubscription);
        }

        if (!isActiveSubscriptionRemoved)
        {
            return;
        }

        // Look up the projection path from the DTO registry
        string? path = ProjectionDtoRegistry.GetPath(projectionType);
        if (path is null)
        {
            // No path registered - cannot unsubscribe properly, but subscription is removed locally
            return;
        }

        await UnsubscribeFromHubAsync(activeSubscription?.SubscriptionId, path, entityId, cancellationToken);
    }

    private async Task OnProjectionUpdatedAsync(
        string path,
        string entityId,
        long newVersion
    )
    {
        // Dispatch message received action for heartbeat/activity indicators
        SignalRMessageReceivedAction messageReceivedAction = new(TimeProvider.GetUtcNow());
        Store.Dispatch(messageReceivedAction);

        // Look up the DTO type for this path
        Type? dtoType = ProjectionDtoRegistry.GetDtoType(path);
        if (dtoType is null)
        {
            // No DTO registered for this path - ignore the update
            return;
        }

        (Type, string) key = (dtoType, entityId);
        if (!activeSubscriptions.TryGetValue(key, out SubscriptionEntry? activeSubscription) ||
            activeSubscription.SubscriptionId is null)
        {
            // Not subscribed to this projection - ignore
            return;
        }

        try
        {
            // Use versioned fetch - we know exactly which version to get.
            // This is more efficient (no extra lookup) and enables better caching
            // since version N of a projection is immutable.
            ProjectionFetchResult? result = await ProjectionFetcher.FetchAtVersionAsync(
                dtoType,
                entityId,
                newVersion,
                CancellationToken.None);
            if (result is not null)
            {
                IAction action = ProjectionActionFactory.CreateUpdated(dtoType, entityId, result.Data, newVersion);
                Store.Dispatch(action);
            }
        }
        catch (Exception ex)
        {
            IAction action = ProjectionActionFactory.CreateError(dtoType, entityId, ex);
            Store.Dispatch(action);
        }
    }

    private async Task OnReconnectedAsync(
        string? connectionId
    )
    {
        _ = connectionId; // Unused but required by delegate signature

        // Re-subscribe to the snapshot of active interests present when this callback starts.
        foreach ((Type ProjectionType, string EntityId) key in activeSubscriptions.Keys)
        {
            // Look up the projection path from the DTO registry
            string? path = ProjectionDtoRegistry.GetPath(key.ProjectionType);
            if (path is null)
            {
                // No path registered - cannot re-subscribe
                continue;
            }

            object request = new();
            bool isClaimed;
            lock (subscriptionGate)
            {
                if (activeSubscriptions.TryGetValue(key, out SubscriptionEntry? activeSubscription) &&
                    activeSubscription.PendingRetryToken is null)
                {
                    // The old ID belongs to the disconnected connection; claim this interest before invoking the hub.
                    activeSubscriptions[key] = new(null, request);
                    isClaimed = true;
                }
                else
                {
                    // Another retry or reconnect already owns this interest, or it was unsubscribed.
                    isClaimed = false;
                }
            }

            if (!isClaimed)
            {
                continue;
            }

            try
            {
                string newSubscriptionId = await HubConnection.InvokeAsync<string>(
                    InletHubConstants.SubscribeMethod,
                    path,
                    key.EntityId,
                    CancellationToken.None);
                bool isCurrentRequest = TryCompleteFailedSubscriptionRetry(key, request, newSubscriptionId);
                if (!isCurrentRequest)
                {
                    // The owner removed this interest while the hub reply was pending.
                    await UnsubscribeFromHubAsync(newSubscriptionId, path, key.EntityId, CancellationToken.None);
                    continue;
                }

                // Refresh the projection data after reconnection. Keep the new ID if this fetch fails.
                ProjectionFetchResult? result = await ProjectionFetcher.FetchAsync(
                    key.ProjectionType,
                    key.EntityId,
                    CancellationToken.None);
                if (result is not null)
                {
                    IAction action = ProjectionActionFactory.CreateUpdated(
                        key.ProjectionType,
                        key.EntityId,
                        result.Data,
                        result.Version);
                    Store.Dispatch(action);
                }
            }
            catch (Exception ex)
            {
                // A failed invocation releases the claim so a later retry can try again.
                _ = TryCompleteFailedSubscriptionRetry(key, request, null);
                IAction action = ProjectionActionFactory.CreateError(key.ProjectionType, key.EntityId, ex);
                Store.Dispatch(action);
            }
        }
    }

    private bool TryCompletePendingSubscription(
        (Type ProjectionType, string EntityId) key,
        object request,
        string? subscriptionId
    )
    {
        lock (subscriptionGate)
        {
            if (!pendingSubscriptionRequests.TryGetValue(key, out HashSet<object>? requests) ||
                !requests.Remove(request))
            {
                return false;
            }

            if (requests.Count == 0)
            {
                pendingSubscriptionRequests.Remove(key);
            }

            if (subscriptionId is not null)
            {
                activeSubscriptions[key] = new(subscriptionId, null);
            }

            return true;
        }
    }

    private bool TryCompleteFailedSubscriptionRetry(
        (Type ProjectionType, string EntityId) key,
        object request,
        string? subscriptionId
    )
    {
        lock (subscriptionGate)
        {
            if (!activeSubscriptions.TryGetValue(key, out SubscriptionEntry? activeSubscription) ||
                !ReferenceEquals(activeSubscription.PendingRetryToken, request))
            {
                return false;
            }

            // A null ID releases the claim while keeping the owner's interest eligible for another attempt.
            activeSubscriptions[key] = new(subscriptionId, null);
            return true;
        }
    }

    private bool TryRegisterOrClaimFailedSubscriptionRetry(
        (Type ProjectionType, string EntityId) key,
        object request,
        out bool isFailedSubscriptionRetry
    )
    {
        lock (subscriptionGate)
        {
            isFailedSubscriptionRetry = false;
            if (activeSubscriptions.TryGetValue(key, out SubscriptionEntry? activeSubscription))
            {
                if (activeSubscription.SubscriptionId is not null || activeSubscription.PendingRetryToken is not null)
                {
                    return false;
                }

                // Claim the failed interest atomically so reconnect and explicit retry cannot overlap it.
                activeSubscriptions[key] = new(null, request);
                isFailedSubscriptionRetry = true;
                return true;
            }

            if (!pendingSubscriptionRequests.TryGetValue(key, out HashSet<object>? requests))
            {
                requests = [];
                pendingSubscriptionRequests.Add(key, requests);
            }

            requests.Add(request);
            return true;
        }
    }

    private async Task UnsubscribeFromHubAsync(
        string? subscriptionId,
        string path,
        string entityId,
        CancellationToken cancellationToken
    )
    {
        if (subscriptionId is null)
        {
            return;
        }

        try
        {
            await HubConnection.InvokeAsync(
                InletHubConstants.UnsubscribeMethod,
                subscriptionId,
                path,
                entityId,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception)
        {
            // Unsubscribe failures are non-fatal - server will clean up on disconnect
        }
    }

#pragma warning restore CA1031
}