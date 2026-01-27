using System;
using System.Collections.Immutable;
using System.Linq;

using Allure.Xunit.Attributes;

using FluentAssertions;

using Spring.Domain.Aggregates.TransactionInvestigationQueue.Events;
using Spring.Domain.Projections.FlaggedTransactions;
using Spring.Domain.Projections.FlaggedTransactions.Reducers;

using Xunit;


namespace Spring.Domain.L0Tests.Projections.FlaggedTransactions;

/// <summary>
///     Tests for <see cref="TransactionFlaggedProjectionReducer" />.
/// </summary>
[AllureParentSuite("Spring Domain")]
[AllureSuite("Projections")]
[AllureSubSuite("FlaggedTransactions")]
public sealed class TransactionFlaggedProjectionReducerTests
{
    private static readonly DateTimeOffset OriginalTimestamp = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FlaggedTimestamp = new(2025, 1, 15, 10, 5, 0, TimeSpan.Zero);

    private readonly TransactionFlaggedProjectionReducer reducer = new();

    /// <summary>
    ///     Reducing TransactionFlagged adds a flagged transaction entry.
    /// </summary>
    [Fact]
    [AllureFeature("Flagged Entries")]
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
        result.Entries.Should().ContainSingle();
        FlaggedTransaction entry = result.Entries[0];
        entry.AccountId.Should().Be("acc-123");
        entry.Amount.Should().Be(15_000m);
        entry.OriginalTimestamp.Should().Be(OriginalTimestamp);
        entry.FlaggedTimestamp.Should().Be(FlaggedTimestamp);
        entry.Sequence.Should().Be(1);
    }

    /// <summary>
    ///     Reducing TransactionFlagged increments the current sequence.
    /// </summary>
    [Fact]
    [AllureFeature("Sequencing")]
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
        result.CurrentSequence.Should().Be(16);
        result.Entries[0].Sequence.Should().Be(16);
    }

    /// <summary>
    ///     New entries are prepended (most recent first).
    /// </summary>
    [Fact]
    [AllureFeature("Ordering")]
    public void ReducePrependsNewEntry()
    {
        // Arrange
        FlaggedTransactionsProjection initial = new()
        {
            Entries =
            [
                new FlaggedTransaction
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
        result.Entries.Should().HaveCount(2);
        result.Entries[0].AccountId.Should().Be("new-acc", "newest entry should be first");
        result.Entries[1].AccountId.Should().Be("old-acc", "older entry should be second");
    }

    /// <summary>
    ///     Flagged entries are capped at MaxEntries (30).
    /// </summary>
    [Fact]
    [AllureFeature("Entry Limits")]
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
        result.Entries.Should().HaveCount(FlaggedTransactionsProjection.MaxEntries);
        result.Entries[0].AccountId.Should().Be("acc-new", "newest entry should be first");
        result.Entries[^1].Sequence.Should().Be(2, "oldest entry (seq 1) should be dropped");
    }

    /// <summary>
    ///     Reducing with null event throws ArgumentNullException.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ReduceWithNullEventThrowsArgumentNullException()
    {
        // Arrange
        FlaggedTransactionsProjection initial = new() { Entries = [], CurrentSequence = 0 };

        // Act
        Action act = () => reducer.Apply(initial, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    ///     Reducing returns a new projection instance (immutability check).
    /// </summary>
    [Fact]
    [AllureFeature("Immutability")]
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
        result.Should().NotBeSameAs(initial);
    }

    /// <summary>
    ///     All event data is correctly mapped to the flagged transaction entry.
    /// </summary>
    [Fact]
    [AllureFeature("Data Mapping")]
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
        entry.AccountId.Should().Be("special-account-id");
        entry.Amount.Should().Be(123_456.78m);
        entry.OriginalTimestamp.Should().Be(customOriginal);
        entry.FlaggedTimestamp.Should().Be(customFlagged);
    }
}
