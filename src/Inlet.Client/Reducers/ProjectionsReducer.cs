using System;

using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Inlet.Client.Abstractions.State;


namespace Mississippi.Inlet.Client.Reducers;

/// <summary>
///     Provides pure reducer methods for updating projection state.
/// </summary>
/// <remarks>
///     <para>
///         These reducers follow the Redux pattern: <c>(state, action) → newState</c>.
///         Each method is a pure function with no side effects.
///     </para>
///     <para>
///         Successful loaded and updated actions are admitted per projection type and entity ID.
///         A lower version leaves the existing entry unchanged, including loading, error and connection fields.
///         Equal and newer versions retain normal success behavior. Empty entries start at version -1,
///         so a first result at version zero is accepted. This does not order unversioned errors or
///         distinguish requests returning the same version.
///     </para>
///     <para>
///         Register these reducers using the source-generated registration methods,
///         which wire up the correct action types for each projection DTO.
///     </para>
/// </remarks>
public static class ProjectionsReducer
{
    /// <summary>
    ///     Reduces state when a projection connection state changes.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="state">The current state.</param>
    /// <param name="action">The connection changed action.</param>
    /// <returns>The new state with updated connection status.</returns>
    public static ProjectionsFeatureState ReduceConnectionChanged<T>(
        ProjectionsFeatureState state,
        ProjectionConnectionChangedAction<T> action
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.WithEntryTransform<T>(
            action.EntityId,
            entry => entry with
            {
                IsConnected = action.IsConnected,
            });
    }

    /// <summary>
    ///     Reduces state when a projection error occurs.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="state">The current state.</param>
    /// <param name="action">The error action.</param>
    /// <returns>The new state with error recorded.</returns>
    public static ProjectionsFeatureState ReduceError<T>(
        ProjectionsFeatureState state,
        ProjectionErrorAction<T> action
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.WithEntryTransform<T>(
            action.EntityId,
            entry => entry with
            {
                Error = action.Error,
                IsLoading = false,
            });
    }

    /// <summary>
    ///     Reduces state when a projection has loaded.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="state">The current state.</param>
    /// <param name="action">The loaded action.</param>
    /// <returns>The new state with projection data.</returns>
    public static ProjectionsFeatureState ReduceLoaded<T>(
        ProjectionsFeatureState state,
        ProjectionLoadedAction<T> action
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.WithEntryTransform<T>(
            action.EntityId,
            entry => action.Version < entry.Version
                ? entry
                : entry with
                {
                    Data = action.Data,
                    Version = action.Version,
                    IsLoading = false,
                    Error = null,
                });
    }

    /// <summary>
    ///     Reduces state when a projection starts loading.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="state">The current state.</param>
    /// <param name="action">The loading action.</param>
    /// <returns>The new state with loading flag set.</returns>
    public static ProjectionsFeatureState ReduceLoading<T>(
        ProjectionsFeatureState state,
        ProjectionLoadingAction<T> action
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.WithEntryTransform<T>(
            action.EntityId,
            entry => entry with
            {
                IsLoading = true,
                Error = null,
            });
    }

    /// <summary>
    ///     Reduces state when a projection has been updated.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="state">The current state.</param>
    /// <param name="action">The updated action.</param>
    /// <returns>The new state with updated projection data.</returns>
    public static ProjectionsFeatureState ReduceUpdated<T>(
        ProjectionsFeatureState state,
        ProjectionUpdatedAction<T> action
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.WithEntryTransform<T>(
            action.EntityId,
            entry => action.Version < entry.Version
                ? entry
                : entry with
                {
                    Data = action.Data,
                    Version = action.Version,
                    IsLoading = false,
                    Error = null,
                });
    }
}