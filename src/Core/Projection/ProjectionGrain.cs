using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;


namespace Mississippi.Core.Projection;

/// <summary>
///     Abstract base class for stateless projection grains that provide access to projection data for a single
///     projection model type.
///     This grain is intended to be used as a lightweight, stateless, read-only façade over
///     <see cref="IProjectionCursorGrain{TModel}" /> and <see cref="IPersistantProjectionSnapshotGrain{TModel}" />
///     grains.
/// </summary>
/// <typeparam name="TModel">
///     The projection model type exposed by this grain.
///     The type must expose a public parameterless constructor so it can be created by the infrastructure.
/// </typeparam>
[StatelessWorker]
public abstract class ProjectionGrain<TModel>
    : IGrainBase,
      IProjectionGrain<TModel>
    where TModel : new()
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionGrain{TModel}" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context associated with this grain instance.</param>
    /// <param name="grainFactory">
    ///     The Orleans grain factory used to obtain references to the projection cursor and snapshot grains
    ///     that back this projection.
    /// </param>
    protected ProjectionGrain(
        IGrainContext grainContext,
        IGrainFactory grainFactory
    )
    {
        GrainContext = grainContext;
        GrainFactory = grainFactory;
    }

    /// <summary>
    ///     Gets the Orleans grain context for this grain instance.
    /// </summary>
    public IGrainContext GrainContext { get; }

    /// <summary>
    ///     Gets or sets the last projection snapshot retrieved for this grain.
    /// </summary>
    /// <remarks>
    ///     This value is stored purely in memory on the current silo and is used as a simple cache to avoid
    ///     repeated calls to the underlying snapshot grain when the projection cursor position has not changed.
    /// </remarks>
    private Immutable<ProjectionSnapshot<TModel>>? CachedSnapshot { get; set; }

    /// <summary>
    ///     Gets the Orleans grain factory used to resolve related projection grains.
    /// </summary>
    private IGrainFactory GrainFactory { get; }

    /// <summary>
    ///     Gets or sets the last known cursor position for the projection associated with this grain.
    /// </summary>
    /// <remarks>
    ///     The value is maintained in memory only and is compared against the cursor position reported by the
    ///     corresponding <see cref="IProjectionCursorGrain{TModel}" /> to decide whether a cached snapshot can be reused.
    ///     A value of <c>-1</c> indicates that no snapshot has been loaded yet.
    /// </remarks>
    private long CursorPosition { get; set; } = -1;

    /// <summary>
    ///     Gets the current projection snapshot for the grain's primary key.
    /// </summary>
    /// <remarks>
    ///     This method first queries the <see cref="IProjectionCursorGrain{TModel}" /> to obtain the current cursor
    ///     position. If the cursor position matches the last known <see cref="CursorPosition" /> and a
    ///     <see cref="CachedSnapshot" /> is available, the cached snapshot is returned. Otherwise, the method fetches
    ///     the latest snapshot from the corresponding <see cref="IPersistantProjectionSnapshotGrain{TModel}" />,
    ///     updates the in-memory cache, and returns the fresh snapshot.
    /// </remarks>
    /// <returns>
    ///     A task that, when completed, provides an immutable <see cref="ProjectionSnapshot{TModel}" /> representing
    ///     the current projection state.
    /// </returns>
    public async Task<Immutable<ProjectionSnapshot<TModel>>> GetAsync()
    {
        IProjectionCursorGrain<TModel> cursor =
            GrainFactory.GetGrain<IProjectionCursorGrain<TModel>>(this.GetPrimaryKeyString());
        long position = await cursor.GetCursorPositionAsync();
        Immutable<ProjectionSnapshot<TModel>>? cached = CachedSnapshot;
        if ((position == CursorPosition) && cached.HasValue)
        {
            return cached.Value;
        }

        IPersistantProjectionSnapshotGrain<TModel> projectionSnapshotGrain =
            GrainFactory.GetGrain<IPersistantProjectionSnapshotGrain<TModel>>(
                this.GetPrimaryKeyString() + "/v" + position);
        Immutable<ProjectionSnapshot<TModel>> data = await projectionSnapshotGrain.GetAsync();
        CursorPosition = position;
        CachedSnapshot = data;
        return data;
    }

    /// <summary>
    ///     Gets the current cursor position for the projection associated with this grain's primary key.
    /// </summary>
    /// <remarks>
    ///     This method delegates to the underlying <see cref="IProjectionCursorGrain{TModel}" /> instance, which tracks
    ///     the logical end (or "cursor") of the projection stream.
    /// </remarks>
    /// <returns>
    ///     A task that, when completed, provides the current cursor position as a <see cref="long" /> value.
    /// </returns>
    public async Task<long> GetCursorPositionAsync()
    {
        IProjectionCursorGrain<TModel> cursor =
            GrainFactory.GetGrain<IProjectionCursorGrain<TModel>>(this.GetPrimaryKeyString());
        return await cursor.GetCursorPositionAsync();
    }
}