using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     High-performance logging helpers for <see cref="AggregateGrainFactory" />.
/// </summary>
internal static partial class AggregateGrainFactoryLoggerExtensions
{
    /// <summary>
    ///     Logs that a generic aggregate grain is being resolved.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="entityId">The entity identifier.</param>
    [LoggerMessage(1, LogLevel.Debug, "Resolving generic aggregate {AggregateType} with entity ID {EntityId}")]
    public static partial void AggregateGrainFactoryResolvingGenericAggregate(
        this ILogger logger,
        string aggregateType,
        string entityId
    );
}