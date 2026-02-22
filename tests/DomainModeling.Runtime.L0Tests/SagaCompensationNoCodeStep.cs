using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests;

/// <summary>
///     Step that fails compensation without providing an error code.
/// </summary>
internal sealed class SagaCompensationNoCodeStep
    : ISagaStep<TestSagaState>,
      ICompensatable<TestSagaState>
{
    /// <inheritdoc />
    public Task<CompensationResult> CompensateAsync(
        TestSagaState state,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            new CompensationResult
            {
                ErrorMessage = "nope",
                Success = false,
            });

    /// <inheritdoc />
    public Task<StepResult> ExecuteAsync(
        TestSagaState state,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(StepResult.Succeeded());
}