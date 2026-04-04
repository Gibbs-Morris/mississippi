using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Step that succeeds and emits a marker event.
/// </summary>
internal sealed class SagaSuccessStep : ISagaStep<TestSagaState>
{
    /// <summary>
    ///     Gets the last execution context observed by the step.
    /// </summary>
    internal static SagaStepExecutionContext? LastExecutionContext { get; private set; }

    /// <summary>
    ///     Clears the captured execution context.
    /// </summary>
    internal static void Reset() => LastExecutionContext = null;

    /// <inheritdoc />
    public Task<StepResult> ExecuteAsync(
        TestSagaState state,
        SagaStepExecutionContext context,
        CancellationToken cancellationToken
    )
    {
        LastExecutionContext = context;
        return Task.FromResult(StepResult.Succeeded(new SagaMarkerEvent()));
    }
}