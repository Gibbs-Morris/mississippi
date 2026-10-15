using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

using Orleans;


namespace Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;

/// <summary>Stores immutable unordered membership facts for one source projection type.</summary>
/// <typeparam name="TSource">The source projection type; this declaration is metadata only.</typeparam>
/// <remarks>Public for composition in named aggregate snapshots. Versions are loading floors, not pinned joins.</remarks>
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State.ReferenceSetState`1")]
public sealed record ReferenceSetState<TSource>
{
    /// <summary>Gets the source identifiers and their versions observed at addition.</summary>
    [Id(0)]
    public ImmutableDictionary<string, long> Members { get; init; } =
        ImmutableDictionary.Create<string, long>(StringComparer.Ordinal);

    /// <summary>Gets the source projection type without storing or querying an instance.</summary>
    [JsonIgnore]
    public Type SourceType => typeof(TSource);
}