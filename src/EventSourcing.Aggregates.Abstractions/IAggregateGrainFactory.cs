using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Defines a factory for resolving aggregate grains by their brook key.
/// </summary>
/// <remarks>
///     <para>
///         This factory provides resolution of aggregate grains using a <see cref="BrookKey" />.
///         This ensures that aggregates are consistently keyed and can be referenced by projections.
///     </para>
/// </remarks>
public interface IAggregateGrainFactory
{
    /// <summary>
    ///     Retrieves an aggregate grain of the specified type using a brook key.
    /// </summary>
    /// <typeparam name="TGrain">The aggregate grain interface type.</typeparam>
    /// <param name="brookKey">The brook key identifying the aggregate's event stream.</param>
    /// <returns>The aggregate grain instance.</returns>
    TGrain GetAggregate<TGrain>(
        BrookKey brookKey
    )
        where TGrain : IAggregateGrain;
}