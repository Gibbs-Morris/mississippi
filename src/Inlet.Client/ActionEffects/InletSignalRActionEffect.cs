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

    private readonly
        Dictionary<(Type ProjectionType, string EntityId), (object Interest, TaskCompletionSource<bool> Attempt)>
        pendingSubscriptions = new();

    private readonly object subscriptionGate = new();

    private long connectionGeneration;

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

            foreach (SubscriptionEntry subscription in activeSubscriptions.Values)
            {
                subscription.PendingRetryToken?.TrySetResult(true);
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

    private sealed record SubscriptionEntry(
        string? SubscriptionId,
        TaskCompletionSource<bool>? PendingRetryToken,
        long ConnectionGeneration,
        object Interest
    );

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

            isCurrentRequest = TryCompletePendingSubscription(key, reservation, subscriptionId, subscribeError is null);
            if (isCurrentRequest && subscribeError is not null)
            {
                // Publish the older failure before another same-pair attempt can replace its state.
                yield return ProjectionActionFactory.CreateError(projectionType, entityId, subscribeError);
                yield break;
            }
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

        long subscriptionGeneration = Volatile.Read(ref connectionGeneration);
        await foreach (IAction action in FetchInitialProjectionAsync(projectionType, entityId, cancellationToken))
        {
            if (!IsCurrentSubscription(key, subscriptionId, subscriptionGeneration))
            {
                yield break;
            }

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
        SubscriptionEntry? activeSubscription;
        bool isActiveSubscriptionRemoved;
        lock (subscriptionGate)
        {
            if (pendingSubscriptions.Remove(key, out (object Interest, TaskCompletionSource<bool> Attempt) pending))
            {
                // Released interest also retires callers waiting on the same attempt.
                pending.Attempt.TrySetResult(true);
            }

            isActiveSubscriptionRemoved = activeSubscriptions.TryRemove(key, out activeSubscription);
            activeSubscription?.PendingRetryToken?.TrySetResult(true);
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

    /// <summary>
    ///     Fetches a notified projection and dispatches its returned data and version together.
    /// </summary>
    /// <param name="path">The registered projection route.</param>
    /// <param name="entityId">The entity whose projection changed.</param>
    /// <param name="newVersion">The announced version requested from the fetcher.</param>
    /// <returns>A task representing the notification update.</returns>
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
                IAction action = ProjectionActionFactory.CreateUpdated(dtoType, entityId, result.Data, result.Version);
                Store.Dispatch(action);
            }
        }
        catch (Exception ex)
        {
            IAction action = ProjectionActionFactory.CreateError(dtoType, entityId, ex);
            Store.Dispatch(action);
        }
    }

    private bool IsCurrentSubscription(
        (Type ProjectionType, string EntityId) key,
        string? subscriptionId,
        long generation
    ) =>
        subscriptionId is not null &&
        (generation == Volatile.Read(ref connectionGeneration)) &&
        activeSubscriptions.TryGetValue(key, out SubscriptionEntry? subscription) &&
        (subscription.ConnectionGeneration == generation) &&
        (subscription.SubscriptionId == subscriptionId);

    /// <summary>
    ///     Advances the connection generation and recovers the interests captured for that connection.
    /// </summary>
    /// <param name="connectionId">The optional identifier supplied by SignalR after reconnecting.</param>
    /// <returns>A task representing recovery of the captured interests.</returns>
    private async Task OnReconnectedAsync(
        string? connectionId
    )
    {
        _ = connectionId;
        ICollection<(Type ProjectionType, string EntityId)> interests;
        long generation;
        lock (subscriptionGate)
        {
            generation = ++connectionGeneration;
            interests = activeSubscriptions.Keys;
        }

        foreach ((Type ProjectionType, string EntityId) key in interests)
        {
            string? path = ProjectionDtoRegistry.GetPath(key.ProjectionType);
            if (path is null)
            {
                continue;
            }

            TaskCompletionSource<bool> request = new(TaskCreationOptions.RunContinuationsAsynchronously);
            bool? isClaimed = TryReserveReconnect(key, request, generation);
            if (isClaimed is null)
            {
                return;
            }

            if (isClaimed is false)
            {
                continue;
            }

            await RecoverSubscriptionAsync(key, path, request, generation);
        }
    }

    /// <summary>
    ///     Establishes and fetches one claimed subscription while rejecting superseded work.
    /// </summary>
    /// <param name="key">The projection and entity pair being recovered.</param>
    /// <param name="path">The registered projection route.</param>
    /// <param name="request">The completion source identifying the recovery attempt.</param>
    /// <param name="generation">The connection generation that owns this recovery.</param>
    /// <returns>A task representing subscription and projection recovery for the pair.</returns>
    private async Task RecoverSubscriptionAsync(
        (Type ProjectionType, string EntityId) key,
        string path,
        TaskCompletionSource<bool> request,
        long generation
    )
    {
        string? newSubscriptionId = null;
        try
        {
            newSubscriptionId = await HubConnection.InvokeAsync<string>(
                InletHubConstants.SubscribeMethod,
                path,
                key.EntityId,
                CancellationToken.None);
            if (!TryCompletePendingSubscription(key, request, newSubscriptionId))
            {
                await UnsubscribeFromHubAsync(newSubscriptionId, path, key.EntityId, CancellationToken.None);
                return;
            }

            // Keep the established ID when only the following fetch fails.
            ProjectionFetchResult? result = await ProjectionFetcher.FetchAsync(
                key.ProjectionType,
                key.EntityId,
                CancellationToken.None);
            if (result is not null && IsCurrentSubscription(key, newSubscriptionId, generation))
            {
                Store.Dispatch(
                    ProjectionActionFactory.CreateUpdated(
                        key.ProjectionType,
                        key.EntityId,
                        result.Data,
                        result.Version));
            }
        }
        catch (Exception ex)
        {
            bool isCurrentRequest = newSubscriptionId is null
                ? TryCompletePendingSubscription(key, request, null, false)
                : IsCurrentSubscription(key, newSubscriptionId, generation);
            if (isCurrentRequest)
            {
                try
                {
                    Store.Dispatch(ProjectionActionFactory.CreateError(key.ProjectionType, key.EntityId, ex));
                }
                finally
                {
                    if (newSubscriptionId is null)
                    {
                        _ = TryCompletePendingSubscription(key, request, null);
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Claims only older interests while holding the subscription gate.
    /// </summary>
    /// <param name="key">The projection and entity pair captured at reconnect.</param>
    /// <param name="request">The completion source owned by this recovery.</param>
    /// <param name="generation">The connection generation requesting recovery.</param>
    /// <returns>True when claimed, false when removed or already current, or null when superseded.</returns>
    private bool? TryReserveReconnect(
        (Type ProjectionType, string EntityId) key,
        TaskCompletionSource<bool> request,
        long generation
    )
    {
        lock (subscriptionGate)
        {
            if (generation != connectionGeneration)
            {
                return null;
            }

            if (activeSubscriptions.TryGetValue(key, out SubscriptionEntry? subscription) &&
                (subscription.ConnectionGeneration < generation))
            {
                // A new connection supersedes pending attempts and IDs from older connections.
                activeSubscriptions[key] = new(null, request, generation, subscription.Interest);
                subscription.PendingRetryToken?.TrySetResult(false);
                return true;
            }

            // This interest was removed or has already been established or claimed on this connection.
            return false;
        }
    }

    /// <summary>
    ///     Completes only the current attempt and records its owned server ID before waking waiters.
    /// </summary>
    /// <param name="key">The projection and entity pair.</param>
    /// <param name="reservation">The completion source identifying this attempt.</param>
    /// <param name="subscriptionId">The server ID, or null when no subscription was established.</param>
    /// <param name="completeAttempt">Whether to wake waiters after a successful reply or consumed failure.</param>
    /// <returns>Whether this attempt still represents the application's interest.</returns>
    private bool TryCompletePendingSubscription(
        (Type ProjectionType, string EntityId) key,
        TaskCompletionSource<bool> reservation,
        string? subscriptionId,
        bool completeAttempt = true
    )
    {
        lock (subscriptionGate)
        {
            if (activeSubscriptions.TryGetValue(key, out SubscriptionEntry? activeSubscription) &&
                ReferenceEquals(activeSubscription.PendingRetryToken, reservation))
            {
                if (activeSubscription.ConnectionGeneration != connectionGeneration)
                {
                    return false;
                }

                if (subscriptionId is not null || completeAttempt)
                {
                    activeSubscriptions[key] = new(
                        subscriptionId,
                        null,
                        activeSubscription.ConnectionGeneration,
                        activeSubscription.Interest);
                }

                if (completeAttempt)
                {
                    reservation.TrySetResult(subscriptionId is not null);
                }

                return true;
            }

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
                activeSubscriptions[key] = new(subscriptionId, null, connectionGeneration, pending.Interest);
            }

            if (completeAttempt)
            {
                reservation.TrySetResult(subscriptionId is not null);
            }

            return true;
        }
    }

    /// <summary>
    ///     Registers new interest while the caller holds the subscription gate.
    /// </summary>
    /// <param name="key">The projection and entity pair without a pending entry.</param>
    /// <param name="reservation">The completion source owned by this attempt.</param>
    /// <param name="interest">The caller's earlier interest, or null for a new caller.</param>
    /// <returns>Whether new interest was registered instead of reviving released interest.</returns>
    private bool TryReserveNewInterest(
        (Type ProjectionType, string EntityId) key,
        TaskCompletionSource<bool> reservation,
        object? interest
    )
    {
        if (interest is not null)
        {
            // This caller's interest was released while its retry was queued.
            return false;
        }

        pendingSubscriptions.Add(key, (new(), reservation));
        return true;
    }

    /// <summary>
    ///     Reserves a failed active interest or retains the caller while its current retry is pending.
    /// </summary>
    /// <param name="key">The projection and entity pair protected by the subscription gate.</param>
    /// <param name="subscription">The current active entry for the pair.</param>
    /// <param name="reservation">The completion source identifying this caller's attempt.</param>
    /// <param name="interest">The interest retained across failed attempts.</param>
    /// <param name="pendingAttempt">The current retry to await when the caller must wait.</param>
    /// <returns>True when claimed, false when established or released, or null when a retry is pending.</returns>
    private bool? TryReserveActiveSubscription(
        (Type ProjectionType, string EntityId) key,
        SubscriptionEntry subscription,
        TaskCompletionSource<bool> reservation,
        ref object? interest,
        out Task<bool>? pendingAttempt
    )
    {
        pendingAttempt = null;
        interest ??= subscription.Interest;
        if (!ReferenceEquals(interest, subscription.Interest) || subscription.SubscriptionId is not null)
        {
            return false;
        }

        pendingAttempt = subscription.PendingRetryToken?.Task;
        if (pendingAttempt is not null)
        {
            return null;
        }

        activeSubscriptions[key] = new(null, reservation, connectionGeneration, subscription.Interest);
        return true;
    }

    /// <summary>
    ///     Decides whether to reserve, coalesce, or wait while holding the subscription gate.
    /// </summary>
    /// <param name="key">The projection and entity pair to reserve.</param>
    /// <param name="reservation">The completion source owned by this attempt.</param>
    /// <param name="cancellationToken">The token used to cancel this caller.</param>
    /// <param name="interest">The interest retained across an earlier failed attempt.</param>
    /// <param name="pendingAttempt">The earlier attempt to await when no decision is available.</param>
    /// <returns>True when reserved, false when coalesced or released, or null when waiting is required.</returns>
    private bool? TryReserveSubscription(
        (Type ProjectionType, string EntityId) key,
        TaskCompletionSource<bool> reservation,
        CancellationToken cancellationToken,
        ref object? interest,
        out Task<bool>? pendingAttempt
    )
    {
        pendingAttempt = null;
        lock (subscriptionGate)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            if (activeSubscriptions.TryGetValue(key, out SubscriptionEntry? activeSubscription))
            {
                return TryReserveActiveSubscription(
                    key,
                    activeSubscription,
                    reservation,
                    ref interest,
                    out pendingAttempt);
            }

            if (!pendingSubscriptions.TryGetValue(
                    key,
                    out (object Interest, TaskCompletionSource<bool> Attempt) pending))
            {
                return TryReserveNewInterest(key, reservation, interest);
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

            pendingAttempt = pending.Attempt.Task;
            return null;
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
            bool? reserved = TryReserveSubscription(
                key,
                reservation,
                cancellationToken,
                ref interest,
                out Task<bool>? pendingAttempt);
            if (reserved.HasValue)
            {
                return reserved.Value;
            }

            try
            {
                if (await pendingAttempt!.WaitAsync(cancellationToken))
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