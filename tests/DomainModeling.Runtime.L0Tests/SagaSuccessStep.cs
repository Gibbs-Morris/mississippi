using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Step that succeeds and emits a marker event.
/// </summary>
internal sealed class SagaSuccessStep : ISagaStep<TestSagaState>
{
    /// <inheritdoc />
    public Task<StepResult> ExecuteAsync(
        TestSagaState state,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(StepResult.Succeeded(new SagaMarkerEvent()));
}