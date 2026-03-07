using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.Spring.Domain.Aggregates.AuthProof.Events;

/// <summary>
///     Event raised when authenticated-only endpoint access is authorized.
/// </summary>
[EventStorageName("SPRING", "AUTHPROOF", "AUTHENTICATEDACCESSRECORDED")]
[GenerateSerializer]
[Alias("Mississippi.Spring.Domain.Aggregates.AuthProof.Events.AuthenticatedAccessRecorded")]
internal sealed record AuthenticatedAccessRecorded;