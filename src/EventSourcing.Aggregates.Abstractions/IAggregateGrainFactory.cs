using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Defines a factory for resolving aggregate grains by their key.
/// </summary>
/// <remarks>
///     <para>
///         This factory provides resolution of aggregate grains using an <see cref="AggregateKey" />
///         or <see cref="BrookKey" />. This ensures that aggregates are consistently keyed and
///         can be referenced by projections.
///     </para>
/// </remarks>
public interface IAggregateGrainFactory
{
    /// <summary>
    ///     Retrieves an aggregate grain of the specified type using an aggregate key.
    /// </summary>
    /// <typeparam name="TGrain">The aggregate grain interface type.</typeparam>
    /// <param name="aggregateKey">The aggregate key identifying the aggregate.</param>
    /// <returns>The aggregate grain instance.</returns>
    TGrain GetAggregate<TGrain>(
        AggregateKey aggregateKey
    )
        where TGrain : IAggregateGrain;

    /// <summary>
    ///     Retrieves an aggregate grain of the specified type using a brook key.
    /// </summary>
    /// <typeparam name="TGrain">The aggregate grain interface type.</typeparam>
    /// <param name="brookKey">The brook key identifying the aggregate's event stream.</param>
    /// <returns>The aggregate grain instance.</returns>
    /// <remarks>
    ///     This overload is provided for framework-level code that already works with brook keys.
    ///     Application code should prefer <see cref="GetAggregate{TGrain}(AggregateKey)" />.
    /// </remarks>
    TGrain GetAggregate<TGrain>(
        BrookKey brookKey
    )
        where TGrain : IAggregateGrain;
}