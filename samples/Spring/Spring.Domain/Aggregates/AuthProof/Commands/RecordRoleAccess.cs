using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace MississippiSamples.Spring.Domain.Aggregates.AuthProof.Commands;

/// <summary>
///     Command used to prove role-protected generated endpoint access.
/// </summary>
[GenerateCommand(Route = "role")]
[GenerateAuthorization(Roles = "auth-proof-operator")]
[GenerateSerializer]
[Alias("MississippiSamples.Spring.Domain.Aggregates.AuthProof.Commands.RecordRoleAccess")]
public sealed record RecordRoleAccess;