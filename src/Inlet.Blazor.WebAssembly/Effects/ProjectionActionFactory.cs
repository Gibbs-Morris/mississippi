using System;

using Mississippi.Inlet.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Inlet.Blazor.WebAssembly.Effects;

/// <summary>
///     Factory for creating projection-related actions.
/// </summary>
/// <remarks>
///     <para>
///         This factory encapsulates the reflection-based creation of generic action types,
///         providing a cleaner API for the <see cref="InletSignalREffect" /> and enabling
///         isolated unit testing.
///     </para>
/// </remarks>
internal static class ProjectionActionFactory
{
    /// <summary>
    ///     Creates an error action for the specified projection type.
    /// </summary>
    /// <param name="projectionType">The projection type.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="error">The error that occurred.</param>
    /// <returns>A <see cref="ProjectionErrorAction{T}" /> for the specified type.</returns>
    public static IAction CreateError(
        Type projectionType,
        string entityId,
        Exception error
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentNullException.ThrowIfNull(entityId);
        ArgumentNullException.ThrowIfNull(error);
        Type actionType = typeof(ProjectionErrorAction<>).MakeGenericType(projectionType);
        return (IAction)Activator.CreateInstance(actionType, entityId, error)!;
    }

    /// <summary>
    ///     Creates a loaded action for the specified projection type.
    /// </summary>
    /// <param name="projectionType">The projection type.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="data">The projection data.</param>
    /// <param name="version">The projection version.</param>
    /// <returns>A <see cref="ProjectionLoadedAction{T}" /> for the specified type.</returns>
    public static IAction CreateLoaded(
        Type projectionType,
        string entityId,
        object? data,
        long version
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentNullException.ThrowIfNull(entityId);
        Type actionType = typeof(ProjectionLoadedAction<>).MakeGenericType(projectionType);
        return (IAction)Activator.CreateInstance(actionType, entityId, data, version)!;
    }

    /// <summary>
    ///     Creates a loading action for the specified projection type.
    /// </summary>
    /// <param name="projectionType">The projection type.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>A <see cref="ProjectionLoadingAction{T}" /> for the specified type.</returns>
    public static IAction CreateLoading(
        Type projectionType,
        string entityId
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentNullException.ThrowIfNull(entityId);
        Type actionType = typeof(ProjectionLoadingAction<>).MakeGenericType(projectionType);
        return (IAction)Activator.CreateInstance(actionType, entityId)!;
    }

    /// <summary>
    ///     Creates an updated action for the specified projection type.
    /// </summary>
    /// <param name="projectionType">The projection type.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="data">The projection data.</param>
    /// <param name="version">The projection version.</param>
    /// <returns>A <see cref="ProjectionUpdatedAction{T}" /> for the specified type.</returns>
    public static IAction CreateUpdated(
        Type projectionType,
        string entityId,
        object? data,
        long version
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentNullException.ThrowIfNull(entityId);
        Type actionType = typeof(ProjectionUpdatedAction<>).MakeGenericType(projectionType);
        return (IAction)Activator.CreateInstance(actionType, entityId, data, version)!;
    }
}