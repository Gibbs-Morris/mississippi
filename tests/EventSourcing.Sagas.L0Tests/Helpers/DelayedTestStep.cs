using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

/// <summary>
///     A test step that has the DelayAfterStep attribute applied.
/// </summary>
/// <remarks>
///     Used to test that the SagaStepCompletedEffect applies the delay
///     after step completion when the attribute is present.
/// </remarks>
[SagaStep(1)]
[DelayAfterStep(5000)]
internal sealed class DelayedTestStep : SagaStepBase<TestSagaState>
{
    /// <inheritdoc />
    public override Task<StepResult> ExecuteAsync(
        ISagaContext context,
        TestSagaState state,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(StepResult.Succeeded());
}