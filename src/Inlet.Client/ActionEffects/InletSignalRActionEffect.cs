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
using Mississippi.Inlet.Server.Abstractions;
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

    private readonly IHubConnectionProvider hubConnectionProvider;

    private readonly Lazy<IInletStore> lazyStore;

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
        this.lazyStore = lazyStore;
        this.hubConnectionProvider = hubConnectionProvider;
        ProjectionFetcher = projectionFetcher;
        ProjectionDtoRegistry = projectionDtoRegistry;
        TimeProvider = timeProvider ?? TimeProvider.System;

        // Subscribe to projection update notifications from the server
        hubCallbackRegistration = hubConnectionProvider.RegisterHandler<string, string, long>(
            InletHubConstants.ProjectionUpdatedMethod,
            OnProjectionUpdatedAsync);

        // Handle reconnection - re-subscribe to all active subscriptions
        hubConnectionProvider.OnReconnected(OnReconnectedAsync);
    }

    private HubConnection HubConnection => hubConnectionProvider.Connection;

    private IProjectionDtoRegistry ProjectionDtoRegistry { get; }

    private IProjectionFetcher ProjectionFetcher { get; }

    /// <summary>
    ///     Gets the store lazily to avoid circular dependency during DI resolution.
    ///     The store resolves action effects during construction, so action effects cannot depend
    ///     on the store directly in their constructor.
    /// </summary>
    private IInletStore Store => lazyStore.Value;

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
        activeSubscriptions.Clear();
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
        await hubConnectionProvider.EnsureConnectedAsync(cancellationToken);

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

        // Already subscribed?
        if (activeSubscriptions.ContainsKey(key))
        {
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

        // Yield loading action
        yield return ProjectionActionFactory.CreateLoading(projectionType, entityId);

        // Subscribe via SignalR hub
        string? subscriptionId = null;
        Exception? subscribeError = null;
        bool cancelled = false;
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

        if (cancelled)
        {
            yield break;
        }

        if (subscribeError is not null)
        {
            yield return ProjectionActionFactory.CreateError(projectionType, entityId, subscribeError);
            yield break;
        }

        activeSubscriptions[key] = subscriptionId!;

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
        if (!activeSubscriptions.TryRemove(key, out string? subscriptionId))
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
#pragma warning restore CA1031
}