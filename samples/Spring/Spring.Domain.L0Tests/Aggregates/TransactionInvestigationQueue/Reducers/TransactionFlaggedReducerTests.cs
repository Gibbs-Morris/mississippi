using Allure.Xunit.Attributes;

using Spring.Domain.Aggregates.TransactionInvestigationQueue;
using Spring.Domain.Aggregates.TransactionInvestigationQueue.Events;
using Spring.Domain.Aggregates.TransactionInvestigationQueue.Reducers;


namespace Spring.Domain.L0Tests.Aggregates.TransactionInvestigationQueue.Reducers;

/// <summary>
///     Tests for <see cref="TransactionFlaggedReducer" />.
/// </summary>
[AllureParentSuite("Spring Domain")]
[AllureSuite("Aggregates")]
[AllureSubSuite("TransactionFlaggedReducer")]
public sealed class TransactionFlaggedReducerTests
{
    private static readonly DateTimeOffset TestTimestamp = new(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);

    private readonly TransactionFlaggedReducer reducer = new();

    /// <summary>
    ///     Reducing TransactionFlagged increments the total flagged count.
    /// </summary>
    [Fact]
    [AllureFeature("State Reduction")]
    public void ReduceIncrementsTotalFlaggedCount()
    {
        // Arrange
        TransactionInvestigationQueueAggregate initial = new()
        {
            TotalFlaggedCount = 5,
        };
        TransactionFlagged evt = new()
        {
            AccountId = "acc-456",
            Amount = 25_000m,
            OriginalTimestamp = TestTimestamp,
            FlaggedTimestamp = DateTimeOffset.UtcNow,
        };

        // Act
        TransactionInvestigationQueueAggregate result = reducer.Apply(initial, evt);

        // Assert
        result.TotalFlaggedCount.Should().Be(6);
    }

    /// <summary>
    ///     Reducing multiple events accumulates the count correctly.
    /// </summary>
    [Fact]
    [AllureFeature("State Reduction")]
    public void ReduceMultipleEventsAccumulatesCount()
    {
        // Arrange
        TransactionInvestigationQueueAggregate initial = new()
        {
            TotalFlaggedCount = 10,
        };
        TransactionFlagged evt1 = new()
        {
            AccountId = "acc-1",
            Amount = 15_000m,
            OriginalTimestamp = TestTimestamp,
            FlaggedTimestamp = DateTimeOffset.UtcNow,
        };
        TransactionFlagged evt2 = new()
        {
            AccountId = "acc-2",
            Amount = 20_000m,
            OriginalTimestamp = TestTimestamp,
            FlaggedTimestamp = DateTimeOffset.UtcNow,
        };

        // Act
        TransactionInvestigationQueueAggregate result1 = reducer.Apply(initial, evt1);
        TransactionInvestigationQueueAggregate result2 = reducer.Apply(result1, evt2);

        // Assert
        result2.TotalFlaggedCount.Should().Be(12);
    }

    /// <summary>
    ///     Reducing TransactionFlagged on null state creates new aggregate with count 1.
    /// </summary>
    [Fact]
    [AllureFeature("State Reduction")]
    public void ReduceOnNullStateCreatesNewAggregateWithCountOne()
    {
        // Arrange
        TransactionFlagged evt = new()
        {
            AccountId = "acc-123",
            Amount = 15_000m,
            OriginalTimestamp = TestTimestamp,
            FlaggedTimestamp = DateTimeOffset.UtcNow,
        };

        // Act
        TransactionInvestigationQueueAggregate result = reducer.Apply(null!, evt);

        // Assert
        result.Should().NotBeNull();
        result.TotalFlaggedCount.Should().Be(1);
    }

    /// <summary>
    ///     Reducing returns a new instance (immutability check).
    /// </summary>
    [Fact]
    [AllureFeature("Immutability")]
    public void ReduceReturnsNewInstance()
    {
        // Arrange
        TransactionInvestigationQueueAggregate initial = new()
        {
            TotalFlaggedCount = 5,
        };
        TransactionFlagged evt = new()
        {
            AccountId = "acc-123",
            Amount = 15_000m,
            OriginalTimestamp = TestTimestamp,
            FlaggedTimestamp = DateTimeOffset.UtcNow,
        };

        // Act
        TransactionInvestigationQueueAggregate result = reducer.Apply(initial, evt);

        // Assert
        result.Should().NotBeSameAs(initial);
    }

    /// <summary>
    ///     Reducing with null event throws ArgumentNullException.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ReduceWithNullEventThrowsArgumentNullException()
    {
        // Arrange
        TransactionInvestigationQueueAggregate initial = new()
        {
            TotalFlaggedCount = 0,
        };

        // Act
        Action act = () => reducer.Apply(initial, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}