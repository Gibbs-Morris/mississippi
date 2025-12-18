using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     High-performance logging extensions for aggregate grains.
/// </summary>
internal static partial class AggregateGrainLoggerExtensions
{
    /// <summary>
    ///     Logs when the aggregate grain is activated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(0, LogLevel.Debug, "Aggregate grain activated: {AggregateKey}")]
    public static partial void Activated(
        this ILogger logger,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when a command has been successfully executed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The type of command executed.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(2, LogLevel.Debug, "Executed command {CommandType} for aggregate {AggregateKey}")]
    public static partial void CommandExecuted(
        this ILogger logger,
        string commandType,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when a command execution fails.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The type of command that failed.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    [LoggerMessage(
        3,
        LogLevel.Warning,
        "Command {CommandType} failed for aggregate {AggregateKey}: {ErrorCode} - {ErrorMessage}")]
    public static partial void CommandFailed(
        this ILogger logger,
        string commandType,
        string aggregateKey,
        string errorCode,
        string errorMessage
    );

    /// <summary>
    ///     Logs when a command is received for processing.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The type of command received.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(1, LogLevel.Debug, "Received command {CommandType} for aggregate {AggregateKey}")]
    public static partial void CommandReceived(
        this ILogger logger,
        string commandType,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when aggregate state hydration begins.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="position">The position to replay from.</param>
    [LoggerMessage(
        4,
        LogLevel.Debug,
        "Hydrating state for aggregate {AggregateKey}, replaying from position {Position}")]
    public static partial void HydratingState(
        this ILogger logger,
        string aggregateKey,
        long position
    );

    /// <summary>
    ///     Logs when no snapshot is available and state will be built from scratch.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(5, LogLevel.Debug, "No snapshot available for aggregate {AggregateKey}, building from scratch")]
    public static partial void NoSnapshotAvailable(
        this ILogger logger,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when reducer hash does not match the snapshot, requiring a rebuild.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="snapshotHash">The hash from the snapshot.</param>
    /// <param name="currentHash">The current reducer hash.</param>
    [LoggerMessage(
        6,
        LogLevel.Information,
        "Reducer hash mismatch for aggregate {AggregateKey}: snapshot has {SnapshotHash}, current is {CurrentHash}")]
    public static partial void ReducerHashMismatch(
        this ILogger logger,
        string aggregateKey,
        string snapshotHash,
        string currentHash
    );

    /// <summary>
    ///     Logs when requesting background snapshot persistence.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="version">The snapshot version.</param>
    [LoggerMessage(
        7,
        LogLevel.Debug,
        "Requesting background snapshot persistence for aggregate {AggregateKey} at version {Version}")]
    public static partial void RequestingSnapshotPersistence(
        this ILogger logger,
        string aggregateKey,
        long version
    );

    /// <summary>
    ///     Logs when a snapshot is successfully loaded.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="version">The snapshot version.</param>
    [LoggerMessage(8, LogLevel.Debug, "Loaded snapshot for aggregate {AggregateKey} at version {Version}")]
    public static partial void SnapshotLoaded(
        this ILogger logger,
        string aggregateKey,
        long version
    );

    /// <summary>
    ///     Logs when state hydration completes.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="finalPosition">The final position after hydration.</param>
    [LoggerMessage(9, LogLevel.Debug, "State hydrated for aggregate {AggregateKey} at position {FinalPosition}")]
    public static partial void StateHydrated(
        this ILogger logger,
        string aggregateKey,
        long finalPosition
    );
}