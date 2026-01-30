using System;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Inlet.Client;

/// <summary>
///     Read-only facade over the Store's <see cref="ProjectionsFeatureState" />.
/// </summary>
/// <remarks>
///     <para>
///         This class provides typed access to projection data stored in the Redux state tree.
///         All writes occur through dispatched actions and reducers.
///     </para>
/// </remarks>
public sealed class ProjectionCache : IProjectionCache
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionCache" /> class.
    /// </summary>
    /// <param name="store">The store containing projection state.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store" /> is null.</exception>
    public ProjectionCache(
        IStore store
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        Store = store;
    }

    private IStore Store { get; }

    /// <inheritdoc />
    public T? GetProjection<T>(
        string entityId
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return GetState().GetProjection<T>(entityId);
    }

    /// <inheritdoc />
    public Exception? GetProjectionError<T>(
        string entityId
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return GetState().GetProjectionError<T>(entityId);
    }

    /// <inheritdoc />
    public IProjectionState<T>? GetProjectionState<T>(
        string entityId
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ProjectionEntry<T>? entry = GetState().GetEntry<T>(entityId);
        if (entry is null)
        {
            return null;
        }

        return new ProjectionStateAdapter<T>(entry);
    }

    /// <inheritdoc />
    public long GetProjectionVersion<T>(
        string entityId
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return GetState().GetProjectionVersion<T>(entityId);
    }

    /// <inheritdoc />
    public bool IsProjectionConnected<T>(
        string entityId
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return GetState().IsProjectionConnected<T>(entityId);
    }

    /// <inheritdoc />
    public bool IsProjectionLoading<T>(
        string entityId
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return GetState().IsProjectionLoading<T>(entityId);
    }

    private ProjectionsFeatureState GetState() => Store.GetState<ProjectionsFeatureState>();

    /// <summary>
    ///     Adapter that wraps a <see cref="ProjectionEntry{T}" /> as <see cref="IProjectionState{T}" />.
    /// </summary>
    private sealed class ProjectionStateAdapter<T> : IProjectionState<T>
        where T : class
    {
        private readonly ProjectionEntry<T> entry;

        public ProjectionStateAdapter(
            ProjectionEntry<T> entry
        ) =>
            this.entry = entry;

        public T? Data => entry.Data;

        public Exception? ErrorException => entry.Error;

        public bool IsConnected => entry.IsConnected;

        public bool IsLoading => entry.IsLoading;

        public long Version => entry.Version;
    }
}