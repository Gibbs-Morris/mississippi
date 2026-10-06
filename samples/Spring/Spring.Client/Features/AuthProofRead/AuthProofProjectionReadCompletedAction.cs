using System;

using Mississippi.Reservoir.Abstractions.Actions;

using MississippiSamples.Spring.Client.Features.AuthProof.Dtos;


namespace MississippiSamples.Spring.Client.Features.AuthProofRead;

/// <summary>Reports the outcome of one correlated protected HTTP read.</summary>
/// <param name="RequestId">Identifier of the originating read attempt.</param>
/// <param name="Data">The actual returned projection, or null when no data was found.</param>
/// <param name="Version">The actual server projection version.</param>
/// <param name="ErrorMessage">The read error, or null when the read succeeded.</param>
internal sealed record AuthProofProjectionReadCompletedAction(
    Guid RequestId,
    AuthProofProjectionDto? Data,
    long Version,
    string? ErrorMessage
) : IAction;