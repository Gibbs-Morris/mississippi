using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

/// <summary>
///     Compensation that succeeds.
/// </summary>
internal sealed class SuccessfulCompensation : SagaCompensationBase<TestSagaState>
{
    /// <inheritdoc />
    public override Task<CompensationResult> CompensateAsync(
        ISagaContext context,
        TestSagaState state,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(CompensationResult.Succeeded());
}