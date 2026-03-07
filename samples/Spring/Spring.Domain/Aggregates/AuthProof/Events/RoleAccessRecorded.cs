using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Aggregates.AuthProof.Events;

/// <summary>
///     Event raised when role-protected endpoint access is authorized.
/// </summary>
[EventStorageName("SPRING", "AUTHPROOF", "ROLEACCESSRECORDED")]
[GenerateSerializer]
[Alias("Spring.Domain.Aggregates.AuthProof.Events.RoleAccessRecorded")]
internal sealed record RoleAccessRecorded;