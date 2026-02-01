using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas.L0Tests.Helpers;

/// <summary>
///     Compensation that throws an exception.
/// </summary>
internal sealed class ExceptionThrowingCompensation : SagaCompensationBase<TestSagaState>
{
    /// <inheritdoc />
    public override Task<CompensationResult> CompensateAsync(
        ISagaContext context,
        TestSagaState state,
        CancellationToken cancellationToken
    ) =>
        throw new InvalidOperationException("Database connection lost");
}