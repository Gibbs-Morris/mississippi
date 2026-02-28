using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Aggregates.AuthProof.Commands;

/// <summary>
///     Command used to prove authenticated-only generated endpoint access.
/// </summary>
[GenerateCommand(Route = "authenticated")]
[GenerateAuthorization]
[GenerateSerializer]
[Alias("Spring.Domain.Aggregates.AuthProof.Commands.RecordAuthenticatedAccess")]
public sealed record RecordAuthenticatedAccess;