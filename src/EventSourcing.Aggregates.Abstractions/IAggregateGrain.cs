using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Marker interface for aggregate grain implementations.
///     Aggregates are single-threaded write processors backed by a brook (event stream).
/// </summary>
/// <remarks>
///     <para>
///         Aggregate grains encapsulate domain logic and state internally. They accept commands,
///         validate business rules against current state, emit events, and persist those events
///         to a brook. The internal state is never exposed; use projections for read queries.
///     </para>
///     <para>
///         Each aggregate instance is identified by a composite key consisting of the brook name
///         (from <see cref="BrookNameAttribute" />) and an entity-specific identifier.
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.Aggregates.IAggregateGrain")]
public interface IAggregateGrain : IGrainWithStringKey
{
}