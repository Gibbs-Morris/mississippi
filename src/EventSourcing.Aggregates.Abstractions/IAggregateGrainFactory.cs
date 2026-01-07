using Orleans;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Defines a factory for resolving aggregate grains by their key.
/// </summary>
/// <remarks>
///     <para>
///         This factory provides resolution of aggregate grains using entity IDs.
///         The brook name is derived from the
///         <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />
///         on the aggregate type.
///     </para>
/// </remarks>
public interface IAggregateGrainFactory
{
    /// <summary>
    ///     Retrieves a custom aggregate grain interface using an entity ID.
    /// </summary>
    /// <typeparam name="TGrain">
    ///     The custom aggregate grain interface type that implements <see cref="IGrainWithStringKey" />.
    /// </typeparam>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The aggregate grain instance.</returns>
    /// <remarks>
    ///     <para>
    ///         Use this overload when you have a custom aggregate grain interface with domain-specific
    ///         methods (e.g., <c>ICounterAggregateGrain</c> with <c>IncrementAsync</c>, <c>DecrementAsync</c>).
    ///         The grain is keyed by entity ID only.
    ///     </para>
    /// </remarks>
    TGrain GetAggregate<TGrain>(
        string entityId
    )
        where TGrain : IGrainWithStringKey;

    /// <summary>
    ///     Retrieves a generic aggregate grain for the specified aggregate type using an entity ID.
    /// </summary>
    /// <typeparam name="TAggregate">
    ///     The aggregate state type, decorated with
    ///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
    /// </typeparam>
    /// <param name="entityId">The entity identifier within the brook.</param>
    /// <returns>The generic aggregate grain instance.</returns>
    /// <remarks>
    ///     <para>
    ///         This method resolves the grain using entity ID only. The brook name is derived
    ///         from the <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />
    ///         on the <typeparamref name="TAggregate" /> type.
    ///     </para>
    /// </remarks>
    IGenericAggregateGrain<TAggregate> GetGenericAggregate<TAggregate>(
        string entityId
    )
        where TAggregate : class;

    /// <summary>
    ///     Retrieves a generic aggregate grain for the specified aggregate type using an aggregate key.
    /// </summary>
    /// <typeparam name="TAggregate">
    ///     The aggregate state type, decorated with
    ///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
    /// </typeparam>
    /// <param name="aggregateKey">The aggregate key identifying the aggregate.</param>
    /// <returns>The generic aggregate grain instance.</returns>
    IGenericAggregateGrain<TAggregate> GetGenericAggregate<TAggregate>(
        AggregateKey aggregateKey
    )
        where TAggregate : class;
}