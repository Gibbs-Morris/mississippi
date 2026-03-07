using System;

using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Aggregates.AuthProof;

/// <summary>
///     Saga state used to prove generated saga endpoint authorization behavior.
/// </summary>
[BrookName("SPRING", "AUTHPROOF", "SAGA")]
[SnapshotStorageName("SPRING", "AUTHPROOF", "SAGASTATE")]
[GenerateSagaEndpoints(
    InputType = typeof(StartAuthProofSagaInput),
    RoutePrefix = "auth-proof",
    FeatureKey = "authProof")]
[GenerateAuthorization(Roles = "auth-proof-operator")]
[GenerateSerializer]
[Alias("Spring.Domain.Aggregates.AuthProof.AuthProofSagaState")]
public sealed record AuthProofSagaState : ISagaState
{
    /// <summary>
    ///     Gets the correlation identifier for the saga instance.
    /// </summary>
    [Id(1)]
    public string? CorrelationId { get; init; }

    /// <summary>
    ///     Gets the index of the last completed step.
    /// </summary>
    [Id(2)]
    public int LastCompletedStepIndex { get; init; } = -1;

    /// <summary>
    ///     Gets the current saga phase.
    /// </summary>
    [Id(3)]
    public SagaPhase Phase { get; init; }

    /// <summary>
    ///     Gets the saga identifier.
    /// </summary>
    [Id(0)]
    public Guid SagaId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the saga started.
    /// </summary>
    [Id(4)]
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    ///     Gets the hash representing the ordered saga steps.
    /// </summary>
    [Id(5)]
    public string? StepHash { get; init; }
}