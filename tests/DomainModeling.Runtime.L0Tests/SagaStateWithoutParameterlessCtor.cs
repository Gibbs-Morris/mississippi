using System;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Test saga state without a parameterless constructor.
/// </summary>
internal sealed record SagaStateWithoutParameterlessCtor : ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaStateWithoutParameterlessCtor" /> class.
    /// </summary>
    /// <param name="name">The saga name.</param>
    public SagaStateWithoutParameterlessCtor(
        string name
    ) =>
        Name = name;

    /// <inheritdoc />
    public string? CorrelationId { get; init; }

    /// <inheritdoc />
    public int LastCompletedStepIndex { get; init; } = -1;

    /// <summary>
    ///     Gets the saga name.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public SagaPhase Phase { get; init; }

    /// <inheritdoc />
    public Guid SagaId { get; init; }

    /// <inheritdoc />
    public DateTimeOffset? StartedAt { get; init; }

    /// <inheritdoc />
    public string? StepHash { get; init; }
}