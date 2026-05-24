using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Step that throws during compensation.
/// </summary>
internal sealed class SagaCompensationThrowingStep
    : ISagaStep<TestSagaState>,
      ICompensatable<TestSagaState>
{
    /// <inheritdoc />
    public Task<CompensationResult> CompensateAsync(
        TestSagaState state,
        CancellationToken cancellationToken
    ) =>
        throw new InvalidOperationException("compensation blew up");

    /// <inheritdoc />
    public Task<StepResult> ExecuteAsync(
        TestSagaState state,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(StepResult.Succeeded());
}