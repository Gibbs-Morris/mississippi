using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace MississippiSamples.Spring.Domain.Aggregates.AuthProof.Commands;

/// <summary>
///     Command used to prove claim-policy generated endpoint access.
/// </summary>
[GenerateCommand(Route = "policy")]
[GenerateAuthorization(Policy = "spring.auth-proof.claim")]
[GenerateSerializer]
[Alias("MississippiSamples.Spring.Domain.Aggregates.AuthProof.Commands.RecordPolicyAccess")]
public sealed record RecordPolicyAccess;