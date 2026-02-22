using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Step that succeeds without compensation support.
/// </summary>
internal sealed class SagaNonCompensatableStep : ISagaStep<TestSagaState>
{
    /// <inheritdoc />
    public Task<StepResult> ExecuteAsync(
        TestSagaState state,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(StepResult.Succeeded());
}