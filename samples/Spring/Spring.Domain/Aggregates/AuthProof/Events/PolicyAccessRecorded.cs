using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.Spring.Domain.Aggregates.AuthProof.Events;

/// <summary>
///     Event raised when claim-policy endpoint access is authorized.
/// </summary>
[EventStorageName("SPRING", "AUTHPROOF", "POLICYACCESSRECORDED")]
[GenerateSerializer]
[Alias("Mississippi.Spring.Domain.Aggregates.AuthProof.Events.PolicyAccessRecorded")]
internal sealed record PolicyAccessRecorded;