using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Step that throws during execution.
/// </summary>
internal sealed class SagaThrowingStep : ISagaStep<TestSagaState>
{
    /// <inheritdoc />
    public Task<StepResult> ExecuteAsync(
        TestSagaState state,
        CancellationToken cancellationToken
    ) =>
        throw new InvalidOperationException("kapow");
}