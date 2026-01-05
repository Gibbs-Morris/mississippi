using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     High-performance logging helpers for <see cref="AggregateGrainFactory" />.
/// </summary>
internal static partial class AggregateGrainFactoryLoggerExtensions
{
    /// <summary>
    ///     Logs that an aggregate grain is being resolved.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="grainType">The grain type name.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(1, LogLevel.Debug, "Resolving aggregate {GrainType} with key {AggregateKey}")]
    public static partial void AggregateGrainFactoryResolvingAggregate(
        this ILogger logger,
        string grainType,
        string aggregateKey
    );
}
