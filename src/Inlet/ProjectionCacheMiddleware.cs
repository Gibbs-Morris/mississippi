using System;
using System.Reflection;

using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet;

/// <summary>
///     Middleware that intercepts projection actions and updates the projection cache.
/// </summary>
public sealed class ProjectionCacheMiddleware : IMiddleware
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionCacheMiddleware" /> class.
    /// </summary>
    /// <param name="projectionCache">The projection cache to update.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="projectionCache" /> is null.</exception>
    public ProjectionCacheMiddleware(
        IProjectionCache projectionCache
    )
    {
        ArgumentNullException.ThrowIfNull(projectionCache);
        ProjectionCache = projectionCache;
    }

    private IProjectionCache ProjectionCache { get; }

    private static bool IsGenericActionOfType(
        Type actualType,
        Type genericDefinition
    ) =>
        actualType.IsGenericType && (actualType.GetGenericTypeDefinition() == genericDefinition);

    /// <inheritdoc />
    public void Invoke(
        IAction action,
        Action<IAction> nextAction
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(nextAction);

        // Update projection cache before passing to next middleware
        HandleProjectionAction(action);
        nextAction(action);
    }

    private void HandleLoadedAction(
        IAction action
    )
    {
        Type actionType = action.GetType();
        Type projectionType = actionType.GenericTypeArguments[0];
        PropertyInfo? entityIdProperty = actionType.GetProperty(nameof(ProjectionLoadedAction<object>.EntityId));
        PropertyInfo? dataProperty = actionType.GetProperty(nameof(ProjectionLoadedAction<object>.Data));
        PropertyInfo? versionProperty = actionType.GetProperty(nameof(ProjectionLoadedAction<object>.Version));
        string? entityId = entityIdProperty?.GetValue(action) as string;
        object? data = dataProperty?.GetValue(action);
        long version = (long)(versionProperty?.GetValue(action) ?? -1L);
        if (entityId is null)
        {
            return;
        }

        // Call SetLoaded<T> via reflection since T is only known at runtime
        MethodInfo? setLoadedMethod = typeof(IProjectionCache).GetMethod(nameof(IProjectionCache.SetLoaded))
            ?.MakeGenericMethod(projectionType);
        setLoadedMethod?.Invoke(ProjectionCache, [entityId, data, version]);
    }

    private void HandleLoadingAction(
        IAction action
    )
    {
        Type actionType = action.GetType();
        Type projectionType = actionType.GenericTypeArguments[0];
        string? entityId = (action as IInletAction)?.EntityId;
        if (entityId is null)
        {
            return;
        }

        // Call SetLoading<T> via reflection since T is only known at runtime
        MethodInfo? setLoadingMethod = typeof(IProjectionCache).GetMethod(nameof(IProjectionCache.SetLoading))
            ?.MakeGenericMethod(projectionType);
        setLoadingMethod?.Invoke(ProjectionCache, [entityId]);
    }

    private void HandleProjectionAction(
        IAction action
    )
    {
        Type actionType = action.GetType();
        if (IsGenericActionOfType(actionType, typeof(ProjectionLoadingAction<>)))
        {
            HandleLoadingAction(action);
        }
        else if (IsGenericActionOfType(actionType, typeof(ProjectionLoadedAction<>)))
        {
            HandleLoadedAction(action);
        }
        else if (IsGenericActionOfType(actionType, typeof(ProjectionUpdatedAction<>)))
        {
            HandleUpdatedAction(action);
        }
    }

    private void HandleUpdatedAction(
        IAction action
    )
    {
        Type actionType = action.GetType();
        Type projectionType = actionType.GenericTypeArguments[0];
        PropertyInfo? entityIdProperty = actionType.GetProperty(nameof(ProjectionUpdatedAction<object>.EntityId));
        PropertyInfo? dataProperty = actionType.GetProperty(nameof(ProjectionUpdatedAction<object>.Data));
        PropertyInfo? versionProperty = actionType.GetProperty(nameof(ProjectionUpdatedAction<object>.Version));
        string? entityId = entityIdProperty?.GetValue(action) as string;
        object? data = dataProperty?.GetValue(action);
        long version = (long)(versionProperty?.GetValue(action) ?? -1L);
        if (entityId is null)
        {
            return;
        }

        // Call SetUpdated<T> via reflection since T is only known at runtime
        MethodInfo? setUpdatedMethod = typeof(IProjectionCache).GetMethod(nameof(IProjectionCache.SetUpdated))
            ?.MakeGenericMethod(projectionType);
        setUpdatedMethod?.Invoke(ProjectionCache, [entityId, data, version]);
    }
}