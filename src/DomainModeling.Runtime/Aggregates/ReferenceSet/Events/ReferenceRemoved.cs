using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Events;

/// <summary>Records a durable membership fact without source content.</summary>
[EventStorageName("MISSISSIPPI", "REFERENCESET", "REMOVED", 1)]
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Events.ReferenceRemoved")]
internal sealed record ReferenceRemoved
{
    /// <summary>Gets the exact source identifier.</summary>
    [Id(0)]
    public required string SourceId { get; init; }
}