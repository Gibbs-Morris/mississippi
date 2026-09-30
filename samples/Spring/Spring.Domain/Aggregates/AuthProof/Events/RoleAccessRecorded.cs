using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace MississippiSamples.Spring.Domain.Aggregates.AuthProof.Events;

/// <summary>
///     Event raised when role-protected endpoint access is authorized.
/// </summary>
[EventStorageName("SPRING", "AUTHPROOF", "ROLEACCESSRECORDED", 1)]
[GenerateSerializer]
[Alias("MississippiSamples.Spring.Domain.Aggregates.AuthProof.Events.RoleAccessRecorded")]
internal sealed record RoleAccessRecorded;