using System;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet.Client;

/// <summary>
///     Notifier that dispatches projection update actions to the store.
/// </summary>
/// <remarks>
///     <para>
///         This class follows the pure Redux pattern: it only dispatches actions.
///         State updates occur through reducers that handle these actions.
///     </para>
/// </remarks>
public sealed class ProjectionNotifier : IProjectionUpdateNotifier
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionNotifier" /> class.
    /// </summary>
    /// <param name="store">The store to dispatch actions to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store" /> is null.</exception>
    public ProjectionNotifier(
        IStore store
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        Store = store;
    }

    private IStore Store { get; }

    /// <inheritdoc />
    public void NotifyConnectionChanged<T>(
        string entityId,
        bool isConnected
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        Store.Dispatch(new ProjectionConnectionChangedAction<T>(entityId, isConnected));
    }

    /// <inheritdoc />
    public void NotifyError<T>(
        string entityId,
        Exception exception
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ArgumentNullException.ThrowIfNull(exception);
        Store.Dispatch(new ProjectionErrorAction<T>(entityId, exception));
    }

    /// <inheritdoc />
    public void NotifyProjectionUpdated<T>(
        string entityId,
        T? data,
        long version
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        Store.Dispatch(new ProjectionUpdatedAction<T>(entityId, data, version));
    }
}