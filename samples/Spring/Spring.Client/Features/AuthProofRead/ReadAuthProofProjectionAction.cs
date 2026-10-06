using System;

using Mississippi.Reservoir.Abstractions.Actions;

using MississippiSamples.Spring.Client.Features.AuthSimulation;


namespace MississippiSamples.Spring.Client.Features.AuthProofRead;

/// <summary>Requests a protected read for one entity and immutable local persona.</summary>
/// <param name="RequestId">Unique identifier for this read attempt.</param>
/// <param name="EntityId">The selected Auth Proof entity.</param>
/// <param name="Persona">The local persona when this intent was dispatched.</param>
internal sealed record ReadAuthProofProjectionAction(
    Guid RequestId,
    string EntityId,
    AuthSimulationState Persona
) : IAction;