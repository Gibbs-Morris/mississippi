using System;

using Microsoft.AspNetCore.Components;

using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Abstractions.Actions;
using Mississippi.Inlet.Abstractions.State;
using Mississippi.Reservoir.Blazor;


namespace Mississippi.Inlet.Blazor.WebAssembly;

/// <summary>
///     Base component for Blazor components that interact with server-synced projections.
/// </summary>
public abstract class InletComponent : StoreComponent
{
    /// <summary>
    ///     Gets or sets the inlet store.
    /// </summary>
    [Inject]
    protected IInletStore InletStore { get; set; } = default!;

    /// <summary>
    ///     Gets projection data for a specific entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The projection data, or null if not loaded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    protected T? GetProjection<T>(
        string entityId
    )
        where T : class =>
        InletStore.GetProjection<T>(entityId);

    /// <summary>
    ///     Gets the error for a projection, if any.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The error, or null if no error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    protected Exception? GetProjectionError<T>(
        string entityId
    )
        where T : class =>
        InletStore.GetProjectionError<T>(entityId);

    /// <summary>
    ///     Gets the full projection state for a specific entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The projection state, or null if not loaded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    protected IProjectionState<T>? GetProjectionState<T>(
        string entityId
    )
        where T : class =>
        InletStore.GetProjectionState<T>(entityId);

    /// <summary>
    ///     Gets whether a projection is connected to the server.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>True if the projection is connected.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    protected bool IsProjectionConnected<T>(
        string entityId
    )
        where T : class =>
        InletStore.IsProjectionConnected<T>(entityId);

    /// <summary>
    ///     Gets whether a projection is currently loading.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>True if the projection is loading.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    protected bool IsProjectionLoading<T>(
        string entityId
    )
        where T : class =>
        InletStore.IsProjectionLoading<T>(entityId);

    /// <summary>
    ///     Refreshes a projection for a specific entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    protected void RefreshProjection<T>(
        string entityId
    )
        where T : class
    {
        Dispatch(new RefreshProjectionAction<T>(entityId));
    }

    /// <summary>
    ///     Subscribes to a projection for a specific entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    protected void SubscribeToProjection<T>(
        string entityId
    )
        where T : class
    {
        Dispatch(new SubscribeToProjectionAction<T>(entityId));
    }

    /// <summary>
    ///     Unsubscribes from a projection for a specific entity.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    protected void UnsubscribeFromProjection<T>(
        string entityId
    )
        where T : class
    {
        Dispatch(new UnsubscribeFromProjectionAction<T>(entityId));
    }
}