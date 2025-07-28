using System.Collections.Immutable;

using Orleans.Concurrency;


namespace Mississippi.Core.Projection;

/// <summary>
///     Abstract base class for stateless projection grains that provide access to projection data.
///     Handles caching and delegation to persistent snapshot grains for efficient data retrieval.
/// </summary>
/// <typeparam name="TModel">The type of the projection model.</typeparam>
[StatelessWorker]
public abstract class ProjectionGrain<TModel>
    : Grain,
      IProjectionGrain<TModel>
    where TModel : new()
{
    private long HeadPosition { get; set; } = -1;

    private Immutable<ProjectionSnapshot<TModel>> CachedSnapshot { get; set; }

    /// <summary>
    ///     Gets the current projection snapshot, handling caching and delegation to persistent storage.
    ///     Checks the head position to determine if cached data is still valid.
    /// </summary>
    /// <returns>An immutable projection snapshot containing the current state.</returns>
    public async Task<Immutable<ProjectionSnapshot<TModel>>> GetAsync()
    {
        IProjectionHeadGrain<TModel>? head =
            GrainFactory.GetGrain<IProjectionHeadGrain<TModel>>(this.GetPrimaryKeyString());
        long position = await head.GetHeadPositionAsync();
        if (position == HeadPosition)
        {
            return CachedSnapshot;
        }

        IPersistantProjectionSnapshotGrain<TModel>? pg =
            GrainFactory.GetGrain<IPersistantProjectionSnapshotGrain<TModel>>(
                this.GetPrimaryKeyString() + "/v" + position);
        Immutable<ProjectionSnapshot<TModel>> data = await pg.GetAsync();
        HeadPosition = position;
        CachedSnapshot = data;
        return data;
    }

    /// <summary>
    ///     Gets the current head position of the projection.
    ///     Delegates to the projection head grain for the actual position value.
    /// </summary>
    /// <returns>The head position as a long value.</returns>
    public Task<long> GetHeadPositionAsync() => throw new NotImplementedException();

    /// <summary>
    ///     Gets the current head position of the projection synchronously.
    ///     Internal helper method that queries the head grain.
    /// </summary>
    /// <returns>The head position as a long value.</returns>
    public async Task<long> GetHeadPosition()
    {
        IProjectionHeadGrain<TModel>? head =
            GrainFactory.GetGrain<IProjectionHeadGrain<TModel>>(this.GetPrimaryKeyString());
        return await head.GetHeadPositionAsync();
    }
}

/// <summary>
///     Interface for root-level projection reducers that process events and update projection state.
///     Provides methods for reducing events into projection state and generating reducer hashes for versioning.
/// </summary>
/// <typeparam name="T">The type of the projection state being reduced.</typeparam>
public interface IRootReducer<T>
{
    /// <summary>
    ///     Reduces an event into the current projection state, producing a new state.
    ///     This method is called for each event to update the projection.
    /// </summary>
    /// <param name="state">The current state of the projection before applying the event.</param>
    /// <param name="eventData">The event data to be processed and applied to the state.</param>
    /// <returns>The new projection state after applying the event.</returns>
    T Reduce(
        T state,
        object eventData
    );

    /// <summary>
    ///     Gets a hash value representing the current version of the reducer logic.
    ///     This hash is used to determine if the projection needs to be rebuilt when reducer logic changes.
    /// </summary>
    /// <returns>A string hash representing the reducer version.</returns>
    string GetReducerHash();
}

/// <summary>
///     Interface for finding appropriate snapshot versions based on position and step values.
///     Provides utility methods for snapshot management in projections.
/// </summary>
public interface ISnapshotVersionFinder
{
    /// <summary>
    ///     Finds the appropriate snapshot position based on a given value and step size.
    ///     Calculates the nearest valid snapshot position that is less than or equal to the specified value.
    /// </summary>
    /// <param name="value">The target value to find a snapshot position for.</param>
    /// <param name="step">The step size between valid snapshot positions. Must be greater than 0.</param>
    /// <returns>The calculated snapshot position, or 0 if step is invalid.</returns>
    public static long FindSnapshot(
        long value,
        long step
    )
    {
        if (step <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(step), "Step must be > 0.");
        }

        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be ≥ 0.");
        }

        // Early-out for 0 to avoid unnecessary work.
        if (value == 0)
        {
            return 0;
        }

        long candidate = (value / step) * step;

        // Ensure the result is strictly less than the original value.
        if (candidate == value)
        {
            candidate -= step;
        }

        // Clamp so we never return a negative.
        return candidate < 0 ? 0 : candidate;
    }
}

/// <summary>
///     Represents a projection event in the Mississippi event sourcing system.
///     Contains event data, metadata, and identifiers for event processing and storage.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.Core.Idea.MississippiProjection")]
public sealed record MississippiProjection
{
    /// <summary>
    ///     Gets the raw event payload.
    /// </summary>
    /// <remarks>
    ///     The application receiving the event is responsible for interpreting the
    ///     payload according to <see cref="DataContentType" />.
    /// </remarks>
    [Id(5)]
    public ImmutableArray<byte> Data { get; init; } = ImmutableArray<byte>.Empty;

    /// <summary>
    ///     Gets the MIME type that describes the <see cref="Data" /> payload.
    ///     Examples include <c>application/octet-stream</c> or <c>application/json</c>.
    /// </summary>
    [Id(4)]
    public string DataContentType { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the unique identifier for the event instance.
    /// </summary>
    [Id(2)]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the logical source (typically a stream name) that produced the event.
    /// </summary>
    [Id(1)]
    public string Source { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the timestamp indicating when the event occurred.
    /// </summary>
    [Id(3)]
    public DateTimeOffset? CreationTime { get; init; }

    /// <summary>
    ///     Gets the semantic type of the event.
    /// </summary>
    [Id(0)]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    ///     Gets or initializes a hash value for the projection event.
    ///     Used for data integrity verification and caching purposes.
    /// </summary>
    /// <value>A string representing the hash of the projection event.</value>
    public string Hash { get; init; } = string.Empty;
}