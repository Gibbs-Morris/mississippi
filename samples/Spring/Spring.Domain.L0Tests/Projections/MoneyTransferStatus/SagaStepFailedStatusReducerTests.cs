using Mississippi.EventSourcing.Sagas.Abstractions;

using Spring.Domain.Projections.MoneyTransferStatus;
using Spring.Domain.Projections.MoneyTransferStatus.Reducers;


namespace Spring.Domain.L0Tests.Projections.MoneyTransferStatus;

/// <summary>
///     Tests for <see cref="SagaStepFailedStatusReducer" />.
/// </summary>
public sealed class SagaStepFailedStatusReducerTests
{
    private readonly SagaStepFailedStatusReducer reducer = new();

    /// <summary>
    ///     Verifies null event throws.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrows()
    {
        MoneyTransferStatusProjection initial = new();
        reducer.ShouldThrow<ArgumentNullException, SagaStepFailed, MoneyTransferStatusProjection>(
            initial,
            null!,
            "eventData");
    }

    /// <summary>
    ///     Verifies errors are captured when a step fails.
    /// </summary>
    [Fact]
    public void ReduceWithStepFailedCapturesError()
    {
        MoneyTransferStatusProjection initial = new();
        SagaStepFailed @event = new()
        {
            StepIndex = 1,
            StepName = "Deposit",
            ErrorCode = "ERR",
            ErrorMessage = "failed",
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        result.ErrorCode.Should().Be("ERR");
        result.ErrorMessage.Should().Be("failed");
    }
}