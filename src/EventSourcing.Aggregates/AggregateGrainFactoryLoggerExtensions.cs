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

    /// <summary>
    ///     Logs that a typed aggregate grain is being resolved by entity ID.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="grainType">The grain type name.</param>
    /// <param name="brookName">The brook name derived from the grain interface.</param>
    /// <param name="entityId">The entity identifier.</param>
    [LoggerMessage(3, LogLevel.Debug, "Resolving aggregate {GrainType} (brook: {BrookName}) with entity ID {EntityId}")]
    public static partial void AggregateGrainFactoryResolvingAggregateByEntityId(
        this ILogger logger,
        string grainType,
        string brookName,
        string entityId
    );

    /// <summary>
    ///     Logs that a generic aggregate grain is being resolved.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="aggregateType">The aggregate type name.</param>
    /// <param name="entityId">The entity identifier.</param>
    [LoggerMessage(2, LogLevel.Debug, "Resolving generic aggregate {AggregateType} with entity ID {EntityId}")]
    public static partial void AggregateGrainFactoryResolvingGenericAggregate(
        this ILogger logger,
        string aggregateType,
        string entityId
    );
}