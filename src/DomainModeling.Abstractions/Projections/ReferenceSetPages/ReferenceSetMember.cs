using Orleans;


namespace Mississippi.DomainModeling.Abstractions.Projections.ReferenceSetPages;

/// <summary>Describes a source ID and the minimum source version observed at addition.</summary>
/// <param name="SourceId">The exact source ID without trimming or case folding.</param>
/// <param name="ObservedSourceVersion">The retained loading floor, not a pinned join.</param>
/// <remarks>Public as the immutable entry in bounded membership results; no source content is included.</remarks>
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Abstractions.Projections.ReferenceSetPages.ReferenceSetMember")]
public sealed record ReferenceSetMember(
    [property: Id(0)] string SourceId,
    [property: Id(1)] long ObservedSourceVersion
);