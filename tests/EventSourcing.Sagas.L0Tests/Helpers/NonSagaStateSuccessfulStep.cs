using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

/// <summary>
///     Test step that succeeds with NonSagaState.
/// </summary>
internal sealed class NonSagaStateSuccessfulStep : SagaStepBase<NonSagaState>
{
    /// <inheritdoc />
    public override Task<StepResult> ExecuteAsync(
        ISagaContext context,
        NonSagaState state,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(StepResult.Succeeded());
}