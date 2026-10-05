using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Default saga step info provider for tests.
/// </summary>
internal sealed class DefaultStepInfoProvider : ISagaStepInfoProvider<TestSagaState>
{
    /// <inheritdoc />
    public IReadOnlyList<SagaStepInfo> Steps { get; } =
    [
        new(0, "Step", typeof(SagaStepMarker), false),
    ];
}