using System;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Orleans;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Factory for resolving aggregate grains by their aggregate key.
/// </summary>
internal sealed class AggregateGrainFactory : IAggregateGrainFactory
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateGrainFactory" /> class.
    /// </summary>
    /// <param name="grainFactory">The Orleans grain factory.</param>
    /// <param name="logger">Logger instance.</param>
    public AggregateGrainFactory(
        IGrainFactory grainFactory,
        ILogger<AggregateGrainFactory> logger
    )
    {
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IGrainFactory GrainFactory { get; }

    private ILogger<AggregateGrainFactory> Logger { get; }

    /// <inheritdoc />
    public TGrain GetAggregate<TGrain>(
        string entityId
    )
        where TGrain : IGrainWithStringKey
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        Logger.AggregateGrainFactoryResolvingAggregate(typeof(TGrain).Name, entityId);
        return GrainFactory.GetGrain<TGrain>(entityId);
    }

    /// <inheritdoc />
    public IGenericAggregateGrain<TAggregate> GetGenericAggregate<TAggregate>(
        string entityId
    )
        where TAggregate : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        Logger.AggregateGrainFactoryResolvingGenericAggregate(typeof(TAggregate).Name, entityId);
        return GrainFactory.GetGrain<IGenericAggregateGrain<TAggregate>>(entityId);
    }

    /// <inheritdoc />
    public IGenericAggregateGrain<TAggregate> GetGenericAggregate<TAggregate>(
        AggregateKey aggregateKey
    )
        where TAggregate : class
    {
        Logger.AggregateGrainFactoryResolvingGenericAggregate(typeof(TAggregate).Name, aggregateKey.EntityId);
        return GrainFactory.GetGrain<IGenericAggregateGrain<TAggregate>>(aggregateKey.EntityId);
    }
}