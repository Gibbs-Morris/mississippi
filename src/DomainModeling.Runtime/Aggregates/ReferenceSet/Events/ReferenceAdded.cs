using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Events;

/// <summary>Records a durable membership fact without source content.</summary>
[EventStorageName("MISSISSIPPI", "REFERENCESET", "ADDED", 1)]
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Events.ReferenceAdded")]
internal sealed record ReferenceAdded
{
    /// <summary>Gets the source version observed at addition.</summary>
    [Id(1)]
    public required long ObservedSourceVersion { get; init; }

    /// <summary>Gets the exact source identifier.</summary>
    [Id(0)]
    public required string SourceId { get; init; }
}