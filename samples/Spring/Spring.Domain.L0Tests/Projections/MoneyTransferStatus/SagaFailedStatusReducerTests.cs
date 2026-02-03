using Mississippi.EventSourcing.Sagas.Abstractions;

using Spring.Domain.Projections.MoneyTransferStatus;
using Spring.Domain.Projections.MoneyTransferStatus.Reducers;


namespace Spring.Domain.L0Tests.Projections.MoneyTransferStatus;

/// <summary>
///     Tests for <see cref="SagaFailedStatusReducer" />.
/// </summary>
public sealed class SagaFailedStatusReducerTests
{
    private readonly SagaFailedStatusReducer reducer = new();

    /// <summary>
    ///     Verifies phase updates to failed.
    /// </summary>
    [Fact]
    public void ReduceWithFailedSetsPhase()
    {
        MoneyTransferStatusProjection initial = new()
        {
            Phase = SagaPhase.Running,
        };
        DateTimeOffset failedAt = new(2026, 2, 3, 11, 45, 0, TimeSpan.Zero);
        SagaFailed @event = new()
        {
            ErrorCode = "Failure",
            ErrorMessage = "Failed to transfer.",
            FailedAt = failedAt,
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        result.Phase.Should().Be(SagaPhase.Failed);
        result.ErrorCode.Should().Be(@event.ErrorCode);
        result.ErrorMessage.Should().Be(@event.ErrorMessage);
        result.CompletedAt.Should().Be(failedAt);
    }

    /// <summary>
    ///     Verifies null event throws.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrows()
    {
        MoneyTransferStatusProjection initial = new();
        reducer.ShouldThrow<ArgumentNullException, SagaFailed, MoneyTransferStatusProjection>(
            initial,
            null!,
            "eventData");
    }
}