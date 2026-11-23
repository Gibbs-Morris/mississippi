using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

using Mississippi.Brooks.Abstractions;

using Orleans;


namespace Mississippi.DomainModeling.Abstractions.Projections.ReferenceSetPages;

/// <summary>Pairs bounded ordinal references and selection metadata from one immutable snapshot.</summary>
/// <typeparam name="TProjection">The named membership projection type.</typeparam>
/// <typeparam name="TSource">The referenced source projection type.</typeparam>
/// <remarks>Public as the typed runtime result. Collection positions do not pin deployed reducer code.</remarks>
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Abstractions.Projections.ReferenceSetPages.ReferenceSetPage`2")]
public sealed record ReferenceSetPage<TProjection, TSource>
{
    /// <summary>Gets the requested limit, normalized to 50 when omitted.</summary>
    [Id(1)]
    public required int Limit { get; init; }

    /// <summary>Gets the requested zero-based offset, including offsets beyond the current end.</summary>
    [Id(0)]
    public required int Offset { get; init; }

    /// <summary>Gets the named projection type without serializing an instance.</summary>
    [JsonIgnore]
    public Type ProjectionType => typeof(TProjection);

    /// <summary>Gets references ordered by exact ordinal source ID, retaining observed source versions.</summary>
    [Id(4)]
    public required ImmutableArray<ReferenceSetMember> References { get; init; }

    /// <summary>Gets the referenced source type without serializing or querying an instance.</summary>
    [JsonIgnore]
    public Type SourceType => typeof(TSource);

    /// <summary>Gets the total membership count at Version.</summary>
    [Id(2)]
    public required int TotalCount { get; init; }

    /// <summary>Gets the collection event position; -1 denotes empty history.</summary>
    [Id(3)]
    public required BrookPosition Version { get; init; }
}