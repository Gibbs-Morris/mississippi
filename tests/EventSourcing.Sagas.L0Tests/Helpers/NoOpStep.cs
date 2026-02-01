using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

/// <summary>
///     No-op step for testing.
/// </summary>
internal sealed class NoOpStep : SagaStepBase<TestSagaState>
{
    /// <inheritdoc />
    public override Task<StepResult> ExecuteAsync(
        ISagaContext context,
        TestSagaState state,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(StepResult.Succeeded());
}