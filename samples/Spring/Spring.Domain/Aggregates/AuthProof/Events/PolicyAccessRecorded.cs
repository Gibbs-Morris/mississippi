using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Aggregates.AuthProof.Events;

/// <summary>
///     Event raised when claim-policy endpoint access is authorized.
/// </summary>
[EventStorageName("SPRING", "AUTHPROOF", "POLICYACCESSRECORDED")]
[GenerateSerializer]
[Alias("Spring.Domain.Aggregates.AuthProof.Events.PolicyAccessRecorded")]
internal sealed record PolicyAccessRecorded;