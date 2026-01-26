using System;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Default implementation of <see cref="ISagaStepInfo" />.
/// </summary>
internal sealed record SagaStepInfo : ISagaStepInfo
{
    /// <inheritdoc />
    public required int Order { get; init; }

    /// <inheritdoc />
    public required Type StepType { get; init; }

    /// <inheritdoc />
    public required string Name { get; init; }

    /// <inheritdoc />
    public TimeSpan? Timeout { get; init; }

    /// <inheritdoc />
    public Type? CompensationType { get; init; }
}
