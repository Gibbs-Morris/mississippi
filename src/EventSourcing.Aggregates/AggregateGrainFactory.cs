using System;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions;

using Orleans;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Factory for resolving aggregate grains by their aggregate key or brook key.
/// </summary>
internal sealed class AggregateGrainFactory : IAggregateGrainFactory
{
    private static readonly Action<ILogger, string, string, Exception?> LogResolvingAggregate =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new(1, nameof(GetAggregate)),
            "Resolving aggregate {GrainType} with key {AggregateKey}");

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
        AggregateKey aggregateKey
    )
        where TGrain : IAggregateGrain
    {
        LogResolvingAggregate(Logger, typeof(TGrain).Name, aggregateKey, null);
        return GrainFactory.GetGrain<TGrain>(aggregateKey.ToBrookKey());
    }

    /// <inheritdoc />
    public TGrain GetAggregate<TGrain>(
        BrookKey brookKey
    )
        where TGrain : IAggregateGrain
    {
        LogResolvingAggregate(Logger, typeof(TGrain).Name, brookKey, null);
        return GrainFactory.GetGrain<TGrain>(brookKey);
    }
}