using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Step that supports compensation but fails.
/// </summary>
internal sealed class SagaCompensationFailStep
    : ISagaStep<TestSagaState>,
      ICompensatable<TestSagaState>
{
    /// <inheritdoc />
    public Task<CompensationResult> CompensateAsync(
        TestSagaState state,
        SagaStepExecutionContext context,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(CompensationResult.Failed("FAIL", "nope"));

    /// <inheritdoc />
    public Task<StepResult> ExecuteAsync(
        TestSagaState state,
        SagaStepExecutionContext context,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(StepResult.Succeeded());
}