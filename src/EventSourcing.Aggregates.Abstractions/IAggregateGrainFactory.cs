using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Defines a factory for resolving aggregate grains by their brook definition and entity identifier.
/// </summary>
/// <remarks>
///     <para>
///         This factory provides type-safe resolution of aggregate grains using the brook definition
///         to construct the appropriate <see cref="BrookKey" />. This ensures that aggregates are
///         consistently keyed and can be referenced by projections using the same brook type.
///     </para>
/// </remarks>
public interface IAggregateGrainFactory
{
    /// <summary>
    ///     Retrieves an aggregate grain of the specified type for the given entity identifier.
    /// </summary>
    /// <typeparam name="TGrain">The aggregate grain interface type.</typeparam>
    /// <typeparam name="TBrook">The brook definition type that identifies the event stream.</typeparam>
    /// <param name="entityId">The unique identifier for the aggregate entity.</param>
    /// <returns>The aggregate grain instance.</returns>
    /// <remarks>
    ///     The grain is keyed using a <see cref="BrookKey" /> constructed from the brook name
    ///     (from <typeparamref name="TBrook" />) and the <paramref name="entityId" />.
    /// </remarks>
    TGrain GetAggregate<TGrain, TBrook>(
        string entityId
    )
        where TGrain : IAggregateGrain
        where TBrook : IBrookDefinition;

    /// <summary>
    ///     Retrieves an aggregate grain of the specified type using a pre-constructed brook key.
    /// </summary>
    /// <typeparam name="TGrain">The aggregate grain interface type.</typeparam>
    /// <param name="brookKey">The brook key identifying the aggregate's event stream.</param>
    /// <returns>The aggregate grain instance.</returns>
    TGrain GetAggregate<TGrain>(
        BrookKey brookKey
    )
        where TGrain : IAggregateGrain;
}