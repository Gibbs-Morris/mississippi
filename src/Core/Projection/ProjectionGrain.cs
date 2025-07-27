using System.Collections.Immutable;

using Orleans.Concurrency;


namespace Mississippi.Core.Projection;

[StatelessWorker]
public abstract class ProjectionGrain<TModel>
    : Grain,
      IProjectionGrain<TModel>
    where TModel : new()
{
    private long HeadPosition { get; set; } = -1;

    private Immutable<ProjectionSnapshot<TModel>> CachedSnapshot { get; set; }

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

    public Task<long> GetHeadPositionAsync() => throw new NotImplementedException();

    public async Task<long> GetHeadPosition()
    {
        IProjectionHeadGrain<TModel>? head =
            GrainFactory.GetGrain<IProjectionHeadGrain<TModel>>(this.GetPrimaryKeyString());
        return await head.GetHeadPositionAsync();
    }
}

public interface IRootReducer<T>
{
    T Reduce(
        T State,
        object @event
    );

    string GetReducerHash();
}

public interface ISnapshotVersionFinder
{
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

    public string Hash { get; init; } = string.Empty;
}