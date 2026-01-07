using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Cascade.Server.Services;

/// <summary>
///     Server-side effect that handles projection subscription actions by fetching from Orleans grains
///     and subscribing to the in-process notification system.
/// </summary>
internal sealed class ServerProjectionSubscriptionEffect
    : IEffect,
      IDisposable
{
    private readonly ConcurrentDictionary<(Type, string), IDisposable> activeSubscriptions = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="ServerProjectionSubscriptionEffect" /> class.
    /// </summary>
    /// <param name="store">The inlet store for dispatching update actions.</param>
    /// <param name="grainFactory">The grain factory for accessing UX projection grains.</param>
    /// <param name="notifier">The server projection notifier for real-time updates.</param>
    public ServerProjectionSubscriptionEffect(
        IInletStore store,
        IUxProjectionGrainFactory grainFactory,
        IServerProjectionNotifier notifier
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(grainFactory);
        ArgumentNullException.ThrowIfNull(notifier);
        Store = store;
        GrainFactory = grainFactory;
        Notifier = notifier;
    }

    private IUxProjectionGrainFactory GrainFactory { get; }

    private IServerProjectionNotifier Notifier { get; }

    private IInletStore Store { get; }

    private static IAction CreateErrorAction(
        Type projectionType,
        string entityId,
        Exception error
    )
    {
        Type actionType = typeof(ProjectionErrorAction<>).MakeGenericType(projectionType);
        return (IAction)Activator.CreateInstance(actionType, entityId, error)!;
    }

    private static IAction CreateLoadedAction(
        Type projectionType,
        string entityId,
        object? data,
        long version
    )
    {
        Type actionType = typeof(ProjectionLoadedAction<>).MakeGenericType(projectionType);
        return (IAction)Activator.CreateInstance(actionType, entityId, data, version)!;
    }

    private static IAction CreateLoadingAction(
        Type projectionType,
        string entityId
    )
    {
        Type actionType = typeof(ProjectionLoadingAction<>).MakeGenericType(projectionType);
        return (IAction)Activator.CreateInstance(actionType, entityId)!;
    }

    private static IAction CreateUpdatedAction(
        Type projectionType,
        string entityId,
        object? data,
        long version
    )
    {
        Type actionType = typeof(ProjectionUpdatedAction<>).MakeGenericType(projectionType);
        return (IAction)Activator.CreateInstance(actionType, entityId, data, version)!;
    }

    /// <inheritdoc />
    public bool CanHandle(
        IAction action
    )
    {
        Type actionType = action.GetType();
        if (!actionType.IsGenericType)
        {
            return false;
        }

        Type genericDef = actionType.GetGenericTypeDefinition();
        return (genericDef == typeof(SubscribeToProjectionAction<>)) ||
               (genericDef == typeof(UnsubscribeFromProjectionAction<>));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (IDisposable subscription in activeSubscriptions.Values)
        {
            subscription.Dispose();
        }

        activeSubscriptions.Clear();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        Type actionType = action.GetType();
        Type genericDef = actionType.GetGenericTypeDefinition();
        Type projectionType = actionType.GetGenericArguments()[0];
        if (genericDef == typeof(SubscribeToProjectionAction<>))
        {
            await foreach (IAction resultAction in HandleSubscribeAsync(
                               projectionType,
                               (IInletAction)action,
                               cancellationToken))
            {
                yield return resultAction;
            }
        }
        else if (genericDef == typeof(UnsubscribeFromProjectionAction<>))
        {
            HandleUnsubscribe(projectionType, (IInletAction)action);
        }
    }

    private async Task<object?> FetchProjectionAsync(
        Type projectionType,
        string entityId
    )
    {
        // Use reflection to call GetUxProjectionGrain<T>(entityId)
        MethodInfo getGrainMethod =
            typeof(IUxProjectionGrainFactory).GetMethod(nameof(IUxProjectionGrainFactory.GetUxProjectionGrain))!
                .MakeGenericMethod(projectionType);
        object grain = getGrainMethod.Invoke(GrainFactory, [entityId])!;

        // Call GetAsync() on the grain
        MethodInfo getAsyncMethod = grain.GetType().GetMethod("GetAsync")!;
        Task task = (Task)getAsyncMethod.Invoke(grain, null)!;
        await task;

        // Get the result from the task
        PropertyInfo resultProp = task.GetType().GetProperty("Result")!;
        return resultProp.GetValue(task);
    }

    private async Task<long> FetchVersionAsync(
        Type projectionType,
        string entityId
    )
    {
        // Use reflection to call GetUxProjectionGrain<T>(entityId)
        MethodInfo getGrainMethod =
            typeof(IUxProjectionGrainFactory).GetMethod(nameof(IUxProjectionGrainFactory.GetUxProjectionGrain))!
                .MakeGenericMethod(projectionType);
        object grain = getGrainMethod.Invoke(GrainFactory, [entityId])!;

        // Call GetVersionAsync() on the grain
        MethodInfo getVersionMethod = grain.GetType().GetMethod("GetVersionAsync")!;
        Task<long> task = (Task<long>)getVersionMethod.Invoke(grain, null)!;
        return await task;
    }

    private async IAsyncEnumerable<IAction> HandleSubscribeAsync(
        Type projectionType,
        IInletAction action,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        _ = cancellationToken; // Suppress unused parameter warning
        string entityId = action.EntityId;
        (Type, string) key = (projectionType, entityId);

        // Already subscribed?
        if (activeSubscriptions.ContainsKey(key))
        {
            yield break;
        }

        // Yield loading action
        yield return CreateLoadingAction(projectionType, entityId);
        object? data;
        long version;
        Exception? fetchError = null;
        try
        {
            // Fetch from Orleans grain using reflection
            data = await FetchProjectionAsync(projectionType, entityId);
            version = await FetchVersionAsync(projectionType, entityId);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected, just stop
            yield break;
        }
        catch (InvalidOperationException ex)
        {
            // Grain or reflection errors
            fetchError = ex;
            data = null;
            version = 0;
        }
        catch (TargetInvocationException ex)
        {
            // Reflection invocation errors - unwrap to get the real exception
            fetchError = ex.InnerException ?? ex;
            data = null;
            version = 0;
        }

        // Cannot yield inside catch - handle error case here
        if (fetchError is not null)
        {
            yield return CreateErrorAction(projectionType, entityId, fetchError);
            yield break;
        }

        // Yield loaded action
        yield return CreateLoadedAction(projectionType, entityId, data, version);

        // Subscribe to updates via notifier (updates dispatch via Store directly)
        string projectionTypeName = projectionType.Name;
        IDisposable subscription = Notifier.Subscribe(
            projectionTypeName,
            entityId,
            args => OnProjectionUpdated(projectionType, entityId, args.NewVersion));
        activeSubscriptions[key] = subscription;
    }

    private void HandleUnsubscribe(
        Type projectionType,
        IInletAction action
    )
    {
        (Type, string) key = (projectionType, action.EntityId);
        if (activeSubscriptions.TryRemove(key, out IDisposable? subscription))
        {
            subscription.Dispose();
        }
    }

    private void OnProjectionUpdated(
        Type projectionType,
        string entityId,
        long newVersion
    )
    {
        // Fire and forget - refetch projection and dispatch update
        // Errors are dispatched as error actions, not propagated
        _ = OnProjectionUpdatedCoreAsync(projectionType, entityId, newVersion);
    }

    private async Task OnProjectionUpdatedCoreAsync(
        Type projectionType,
        string entityId,
        long newVersion
    )
    {
        try
        {
            // Refetch the projection data
            object? data = await FetchProjectionAsync(projectionType, entityId);

            // Dispatch updated action via the injected store
            IAction action = CreateUpdatedAction(projectionType, entityId, data, newVersion);
            Store.Dispatch(action);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown, don't dispatch error
        }
        catch (InvalidOperationException ex)
        {
            IAction action = CreateErrorAction(projectionType, entityId, ex);
            Store.Dispatch(action);
        }
    }
}