using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Step that supports compensation and succeeds.
/// </summary>
internal sealed class SagaCompensationSuccessStep
    : ISagaStep<TestSagaState>,
      ICompensatable<TestSagaState>
{
    /// <summary>
    ///     Gets the last compensation context observed by the step.
    /// </summary>
    internal SagaStepExecutionContext? LastCompensationContext { get; private set; }

    /// <inheritdoc />
    public Task<CompensationResult> CompensateAsync(
        TestSagaState state,
        SagaStepExecutionContext context,
        CancellationToken cancellationToken
    )
    {
        LastCompensationContext = context;
        return Task.FromResult(CompensationResult.Succeeded());
    }

    /// <inheritdoc />
    public Task<StepResult> ExecuteAsync(
        TestSagaState state,
        SagaStepExecutionContext context,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(StepResult.Succeeded());
}