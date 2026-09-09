using System.Collections.Immutable;
using System.Linq;

using MississippiSamples.Spring.Domain.Aggregates.TransactionInvestigationQueue.Events;
using MississippiSamples.Spring.Domain.Projections.FlaggedTransactions;
using MississippiSamples.Spring.Domain.Projections.FlaggedTransactions.Reducers;


namespace MississippiSamples.Spring.Domain.L0Tests.Projections.FlaggedTransactions;

/// <summary>
///     Tests for <see cref="TransactionFlaggedProjectionReducer" />.
/// </summary>
public sealed class TransactionFlaggedProjectionReducerTests
{
    private static readonly DateTimeOffset FlaggedTimestamp = new(2025, 1, 15, 10, 5, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset OriginalTimestamp = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly TransactionFlaggedProjectionReducer reducer = new();

    /// <summary>
    ///     Reducing TransactionFlagged adds a flagged transaction entry.
    /// </summary>
    [Fact]
    public void ReduceAddsFlaggedTransactionEntry()
    {
        // Arrange
        FlaggedTransactionsProjection initial = new()
        {
            Entries = [],
            CurrentSequence = 0,
        };
        TransactionFlagged evt = new()
        {
            AccountId = "acc-123",
            Amount = 15_000m,
            OriginalTimestamp = OriginalTimestamp,
            FlaggedTimestamp = FlaggedTimestamp,
        };

        // Act
        FlaggedTransactionsProjection result = reducer.Apply(initial, evt);

        // Assert
        FlaggedTransaction entry = Assert.Single(result.Entries);
        Assert.Equal("acc-123", entry.AccountId);
        Assert.Equal(15_000m, entry.Amount);
        Assert.Equal(OriginalTimestamp, entry.OriginalTimestamp);
        Assert.Equal(FlaggedTimestamp, entry.FlaggedTimestamp);
        Assert.Equal(1, entry.Sequence);
    }

    /// <summary>
    ///     Flagged entries are capped at MaxEntries (30).
    /// </summary>
    [Fact]
    public void ReduceCapsEntriesAtMaxEntries()
    {
        // Arrange - entries stored most-recent-first (descending sequence order)
        ImmutableArray<FlaggedTransaction> existingEntries = Enumerable.Range(1, 30)
            .Reverse()
            .Select(i => new FlaggedTransaction
            {
                AccountId = $"acc-{i}",
                Amount = i * 1000m,
                OriginalTimestamp = OriginalTimestamp.AddMinutes(i),
                FlaggedTimestamp = FlaggedTimestamp.AddMinutes(i),
                Sequence = i,
            })
            .ToImmutableArray();
        FlaggedTransactionsProjection initial = new()
        {
            Entries = existingEntries,
            CurrentSequence = 30,
        };
        TransactionFlagged evt = new()
        {
            AccountId = "acc-new",
            Amount = 99_999m,
            OriginalTimestamp = OriginalTimestamp,
            FlaggedTimestamp = FlaggedTimestamp,
        };

        // Act
        FlaggedTransactionsProjection result = reducer.Apply(initial, evt);

        // Assert
        Assert.Equal(FlaggedTransactionsProjection.MaxEntries, result.Entries.Length);
        Assert.Equal("acc-new", result.Entries[0].AccountId);
        Assert.Equal(2, result.Entries[^1].Sequence);
    }

    /// <summary>
    ///     Reducing TransactionFlagged increments the current sequence.
    /// </summary>
    [Fact]
    public void ReduceIncrementsCurrentSequence()
    {
        // Arrange
        FlaggedTransactionsProjection initial = new()
        {
            Entries = [],
            CurrentSequence = 15,
        };
        TransactionFlagged evt = new()
        {
            AccountId = "acc-456",
            Amount = 25_000m,
            OriginalTimestamp = OriginalTimestamp,
            FlaggedTimestamp = FlaggedTimestamp,
        };

        // Act
        FlaggedTransactionsProjection result = reducer.Apply(initial, evt);

        // Assert
        Assert.Equal(16, result.CurrentSequence);
        Assert.Equal(16, result.Entries[0].Sequence);
    }

    /// <summary>
    ///     All event data is correctly mapped to the flagged transaction entry.
    /// </summary>
    [Fact]
    public void ReduceMapsAllEventDataCorrectly()
    {
        // Arrange
        DateTimeOffset customOriginal = new(2025, 6, 15, 14, 30, 0, TimeSpan.FromHours(2));
        DateTimeOffset customFlagged = new(2025, 6, 15, 14, 35, 0, TimeSpan.FromHours(2));
        FlaggedTransactionsProjection initial = new()
        {
            Entries = [],
            CurrentSequence = 0,
        };
        TransactionFlagged evt = new()
        {
            AccountId = "special-account-id",
            Amount = 123_456.78m,
            OriginalTimestamp = customOriginal,
            FlaggedTimestamp = customFlagged,
        };

        // Act
        FlaggedTransactionsProjection result = reducer.Apply(initial, evt);

        // Assert
        FlaggedTransaction entry = result.Entries[0];
        Assert.Equal("special-account-id", entry.AccountId);
        Assert.Equal(123_456.78m, entry.Amount);
        Assert.Equal(customOriginal, entry.OriginalTimestamp);
        Assert.Equal(customFlagged, entry.FlaggedTimestamp);
    }

    /// <summary>
    ///     New entries are prepended (most recent first).
    /// </summary>
    [Fact]
    public void ReducePrependsNewEntry()
    {
        // Arrange
        FlaggedTransactionsProjection initial = new()
        {
            Entries =
            [
                new()
                {
                    AccountId = "old-acc",
                    Amount = 12_000m,
                    OriginalTimestamp = OriginalTimestamp,
                    FlaggedTimestamp = FlaggedTimestamp,
                    Sequence = 1,
                },
            ],
            CurrentSequence = 1,
        };
        TransactionFlagged evt = new()
        {
            AccountId = "new-acc",
            Amount = 20_000m,
            OriginalTimestamp = OriginalTimestamp.AddHours(1),
            FlaggedTimestamp = FlaggedTimestamp.AddHours(1),
        };

        // Act
        FlaggedTransactionsProjection result = reducer.Apply(initial, evt);

        // Assert
        Assert.Equal(2, result.Entries.Length);
        Assert.Equal("new-acc", result.Entries[0].AccountId);
        Assert.Equal("old-acc", result.Entries[1].AccountId);
    }

    /// <summary>
    ///     Reducing returns a new projection instance (immutability check).
    /// </summary>
    [Fact]
    public void ReduceReturnsNewInstance()
    {
        // Arrange
        FlaggedTransactionsProjection initial = new()
        {
            Entries = [],
            CurrentSequence = 0,
        };
        TransactionFlagged evt = new()
        {
            AccountId = "acc-123",
            Amount = 15_000m,
            OriginalTimestamp = OriginalTimestamp,
            FlaggedTimestamp = FlaggedTimestamp,
        };

        // Act
        FlaggedTransactionsProjection result = reducer.Apply(initial, evt);

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
        FlaggedTransactionsProjection initial = new()
        {
            Entries = [],
            CurrentSequence = 0,
        };

        // Act
        Action act = () => reducer.Apply(initial, null!);

        // Assert
        Assert.ThrowsAny<ArgumentNullException>(act);
    }
}