using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

/// <summary>
///     Compensation that fails.
/// </summary>
internal sealed class FailingCompensation : SagaCompensationBase<TestSagaState>
{
    /// <inheritdoc />
    public override Task<CompensationResult> CompensateAsync(
        ISagaContext context,
        TestSagaState state,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(CompensationResult.Failed("ROLLBACK_FAILED", "Cannot reverse transaction"));
}