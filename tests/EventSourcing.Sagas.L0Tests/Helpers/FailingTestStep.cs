using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

/// <summary>
///     Test step that fails with a specific error code.
/// </summary>
internal sealed class FailingTestStep : SagaStepBase<TestSagaState>
{
    /// <inheritdoc />
    public override Task<StepResult> ExecuteAsync(
        ISagaContext context,
        TestSagaState state,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(StepResult.Failed("PAYMENT_DECLINED", "Insufficient funds"));
}