using Mississippi.EventSourcing.Sagas.Abstractions;

using Spring.Domain.Projections.MoneyTransferStatus;
using Spring.Domain.Projections.MoneyTransferStatus.Reducers;


namespace Spring.Domain.L0Tests.Projections.MoneyTransferStatus;

/// <summary>
///     Tests for <see cref="SagaCompensatingStatusReducer" />.
/// </summary>
public sealed class SagaCompensatingStatusReducerTests
{
    private readonly SagaCompensatingStatusReducer reducer = new();

    /// <summary>
    ///     Verifies phase updates to compensating.
    /// </summary>
    [Fact]
    public void ReduceWithCompensatingSetsPhase()
    {
        MoneyTransferStatusProjection initial = new()
        {
            Phase = SagaPhase.Running,
        };
        SagaCompensating @event = new()
        {
            FromStepIndex = 0,
        };
        MoneyTransferStatusProjection result = reducer.Apply(initial, @event);
        result.Phase.Should().Be(SagaPhase.Compensating);
    }

    /// <summary>
    ///     Verifies null event throws.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrows()
    {
        MoneyTransferStatusProjection initial = new();
        reducer.ShouldThrow<ArgumentNullException, SagaCompensating, MoneyTransferStatusProjection>(
            initial,
            null!,
            "eventData");
    }
}