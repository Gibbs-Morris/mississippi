using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Step that reports skipped compensation.
/// </summary>
internal sealed class SagaCompensationSkippedStep
    : ISagaStep<TestSagaState>,
      ICompensatable<TestSagaState>
{
    /// <inheritdoc />
    public Task<CompensationResult> CompensateAsync(
        TestSagaState state,
        SagaStepExecutionContext context,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(CompensationResult.SkippedResult("skipped"));

    /// <inheritdoc />
    public Task<StepResult> ExecuteAsync(
        TestSagaState state,
        SagaStepExecutionContext context,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(StepResult.Succeeded());
}