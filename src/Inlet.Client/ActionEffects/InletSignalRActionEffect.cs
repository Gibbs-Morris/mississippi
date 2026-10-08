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
    private readonly ConcurrentDictionary<(Type ProjectionType, string EntityId), string> activeSubscriptions = new();

    private readonly IDisposable hubCallbackRegistration;

    private readonly
        Dictionary<(Type ProjectionType, string EntityId), (object Interest, TaskCompletionSource<bool> Attempt)>
        pendingSubscriptions = new();

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
            foreach ((object Interest, TaskCompletionSource<bool> Attempt) pending in pendingSubscriptions.Values)
            {
                pending.Attempt.TrySetResult(true);
            }

            pendingSubscriptions.Clear();
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
        if (activeSubscriptions.ContainsKey(key))
        {
            // Already subscribed
            yield break;
        }

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

        TaskCompletionSource<bool> reservation = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!await TryReserveSubscriptionAsync(key, reservation, cancellationToken))
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

            isCurrentRequest = TryCompletePendingSubscription(key, reservation, subscriptionId);
        }
        finally
        {
            // Iterator disposal and failed invocations also release the pending request.
            _ = TryCompletePendingSubscription(key, reservation, null);
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

        await foreach (IAction action in FetchInitialProjectionAsync(projectionType, entityId, cancellationToken))
        {
            yield return action;
        }
    }

    /// <summary>
    ///     Fetches initial data after the server subscription has been established.
    /// </summary>
    /// <param name="projectionType">The registered projection DTO type.</param>
    /// <param name="entityId">The subscribed entity identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the fetch.</param>
    /// <returns>The loaded or error action produced by the initial fetch.</returns>
    private async IAsyncEnumerable<IAction> FetchInitialProjectionAsync(
        Type projectionType,
        string entityId,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
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
        string? subscriptionId;
        bool isActiveSubscriptionRemoved;
        lock (subscriptionGate)
        {
            if (pendingSubscriptions.Remove(key, out (object Interest, TaskCompletionSource<bool> Attempt) pending))
            {
                // Released interest also retires callers waiting on the same attempt.
                pending.Attempt.TrySetResult(true);
            }

            isActiveSubscriptionRemoved = activeSubscriptions.TryRemove(key, out subscriptionId);
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

        await UnsubscribeFromHubAsync(subscriptionId!, path, entityId, cancellationToken);
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
        if (!activeSubscriptions.ContainsKey(key))
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

        // Re-subscribe to all active subscriptions after reconnection
        foreach ((Type ProjectionType, string EntityId) key in activeSubscriptions.Keys)
        {
            // Look up the projection path from the DTO registry
            string? path = ProjectionDtoRegistry.GetPath(key.ProjectionType);
            if (path is null)
            {
                // No path registered - cannot re-subscribe
                continue;
            }

            try
            {
                string newSubscriptionId = await HubConnection.InvokeAsync<string>(
                    InletHubConstants.SubscribeMethod,
                    path,
                    key.EntityId,
                    CancellationToken.None);
                activeSubscriptions[key] = newSubscriptionId;

                // Refresh the projection data after reconnection
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
                IAction action = ProjectionActionFactory.CreateError(key.ProjectionType, key.EntityId, ex);
                Store.Dispatch(action);
            }
        }
    }

    /// <summary>
    ///     Completes only the current attempt and records its owned server ID before waking waiters.
    /// </summary>
    /// <param name="key">The projection and entity pair.</param>
    /// <param name="reservation">The completion source identifying this attempt.</param>
    /// <param name="subscriptionId">The server ID, or null when no subscription was established.</param>
    /// <returns>Whether this attempt still represents the application's interest.</returns>
    private bool TryCompletePendingSubscription(
        (Type ProjectionType, string EntityId) key,
        TaskCompletionSource<bool> reservation,
        string? subscriptionId
    )
    {
        lock (subscriptionGate)
        {
            if (!pendingSubscriptions.TryGetValue(
                    key,
                    out (object Interest, TaskCompletionSource<bool> Attempt) pending) ||
                !ReferenceEquals(pending.Attempt, reservation))
            {
                return false;
            }

            if (subscriptionId is not null)
            {
                pendingSubscriptions.Remove(key);
                activeSubscriptions[key] = subscriptionId;
            }

            reservation.TrySetResult(subscriptionId is not null);
            return true;
        }
    }

    /// <summary>
    ///     Reserves a pair after failed attempts while coalescing established or released interest.
    /// </summary>
    /// <param name="key">The projection and entity pair to reserve.</param>
    /// <param name="reservation">The completion source owned by this attempt.</param>
    /// <param name="cancellationToken">The token used to cancel waiting for an earlier attempt.</param>
    /// <returns>Whether this caller owns the reservation and should attempt subscription.</returns>
    private async Task<bool> TryReserveSubscriptionAsync(
        (Type ProjectionType, string EntityId) key,
        TaskCompletionSource<bool> reservation,
        CancellationToken cancellationToken
    )
    {
        object? interest = null;
        while (true)
        {
            (object Interest, TaskCompletionSource<bool> Attempt) pending;
            lock (subscriptionGate)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                if (activeSubscriptions.ContainsKey(key))
                {
                    return false;
                }

                if (!pendingSubscriptions.TryGetValue(key, out pending))
                {
                    if (interest is not null)
                    {
                        // This caller's interest was released while its retry was queued.
                        return false;
                    }

                    pendingSubscriptions.Add(key, (new(), reservation));
                    return true;
                }

                interest ??= pending.Interest;
                if (!ReferenceEquals(interest, pending.Interest))
                {
                    return false;
                }

                // Successful and released attempts are removed; a retained completed attempt failed.
                if (pending.Attempt.Task.IsCompletedSuccessfully)
                {
                    pendingSubscriptions[key] = (interest, reservation);
                    return true;
                }
            }

            try
            {
                if (await pending.Attempt.Task.WaitAsync(cancellationToken))
                {
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
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