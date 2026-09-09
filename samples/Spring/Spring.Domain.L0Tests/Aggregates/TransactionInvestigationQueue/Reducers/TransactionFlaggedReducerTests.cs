using MississippiSamples.Spring.Domain.Aggregates.TransactionInvestigationQueue;
using MississippiSamples.Spring.Domain.Aggregates.TransactionInvestigationQueue.Events;
using MississippiSamples.Spring.Domain.Aggregates.TransactionInvestigationQueue.Reducers;


namespace MississippiSamples.Spring.Domain.L0Tests.Aggregates.TransactionInvestigationQueue.Reducers;

/// <summary>
///     Tests for <see cref="TransactionFlaggedReducer" />.
/// </summary>
public sealed class TransactionFlaggedReducerTests
{
    private static readonly DateTimeOffset TestTimestamp = new(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);

    private readonly TransactionFlaggedReducer reducer = new();

    /// <summary>
    ///     Reducing TransactionFlagged increments the total flagged count.
    /// </summary>
    [Fact]
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
            FlaggedTimestamp = TestTimestamp.AddMinutes(5),
        };

        // Act
        TransactionInvestigationQueueAggregate result = reducer.Apply(initial, evt);

        // Assert
        Assert.Equal(6, result.TotalFlaggedCount);
    }

    /// <summary>
    ///     Reducing multiple events accumulates the count correctly.
    /// </summary>
    [Fact]
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
            FlaggedTimestamp = TestTimestamp.AddMinutes(5),
        };
        TransactionFlagged evt2 = new()
        {
            AccountId = "acc-2",
            Amount = 20_000m,
            OriginalTimestamp = TestTimestamp,
            FlaggedTimestamp = TestTimestamp.AddMinutes(10),
        };

        // Act
        TransactionInvestigationQueueAggregate result1 = reducer.Apply(initial, evt1);
        TransactionInvestigationQueueAggregate result2 = reducer.Apply(result1, evt2);

        // Assert
        Assert.Equal(12, result2.TotalFlaggedCount);
    }

    /// <summary>
    ///     Reducing TransactionFlagged on null state creates new aggregate with count 1.
    /// </summary>
    [Fact]
    public void ReduceOnNullStateCreatesNewAggregateWithCountOne()
    {
        // Arrange
        TransactionFlagged evt = new()
        {
            AccountId = "acc-123",
            Amount = 15_000m,
            OriginalTimestamp = TestTimestamp,
            FlaggedTimestamp = TestTimestamp.AddMinutes(5),
        };

        // Act
        TransactionInvestigationQueueAggregate result = reducer.Apply(null!, evt);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalFlaggedCount);
    }

    /// <summary>
    ///     Reducing returns a new instance (immutability check).
    /// </summary>
    [Fact]
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
            FlaggedTimestamp = TestTimestamp.AddMinutes(5),
        };

        // Act
        TransactionInvestigationQueueAggregate result = reducer.Apply(initial, evt);

        // Assert
        Assert.NotSame(initial, result);
    }

    /// <summary>
    ///     Reducing with null event throws ArgumentNullException.
    /// </summary>
    [Fact]
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
        Assert.ThrowsAny<ArgumentNullException>(act);
    }
}