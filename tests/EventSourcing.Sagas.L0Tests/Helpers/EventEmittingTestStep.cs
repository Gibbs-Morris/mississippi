using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

/// <summary>
///     Test step that emits business events.
/// </summary>
internal sealed class EventEmittingTestStep : SagaStepBase<TestSagaState>
{
    /// <inheritdoc />
    public override Task<StepResult> ExecuteAsync(
        ISagaContext context,
        TestSagaState state,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            StepResult.Succeeded(
                new List<object>
                {
                    new TestBusinessEvent("Order processed"),
                }));
}