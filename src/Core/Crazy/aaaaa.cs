namespace Mississippi.Core.Crazy;

// as a dev you need to define
// projection
// reducers
// events
// logic to convert command to events.
// agg level commands
// saga commands - need a saga pattern.

public interface IProjectionGrain<TProjection>
{
    // Return model
    Task<ProjectionSnapshop<TProjection>> GetProjectionAsync();

    // Just return meta data
    Task<ProjectionMetaData> GetMetaDataAsync();
}

public record ProjectionMetaData
{
    // current version
    public required long Version { get; init; }

    // Path to projectino
    public required string Path { get; init; }

    // Path to Agg ie stream name.
    public required string AggregatePath { get; init; }
}

public sealed record ProjectionSnapshop<TProjection> : ProjectionMetaData
{
    public required TProjection Snapshot { get; init; }
}

//Write  -- a single stream behind it.
public interface IAggregteGrain<TCommandMarker>
{
    Task RunCommandAsync(
        TCommandMarker Command
    );

    Task RunCommandAsync(
        TCommandMarker Command,
        long expectedVersion
    );

    Task RunTransactionCommandAsync(
        TCommandMarker Command
    );

    Task RunTransactionCommandAsync(
        TCommandMarker Command,
        long expectedVersion
    );
}

public interface CommandHandler<TCommand>
{
    Task HandleAsync<TCommand>(
        TCommand command
    );
}

public enum eCommandType
{
    System = 0,

    AggregrateRoot = 1,
}