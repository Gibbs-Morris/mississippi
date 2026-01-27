using System;
using System.Collections.Immutable;
using System.Linq;

using Allure.Xunit.Attributes;

using FluentAssertions;

using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Projections.BankAccountLedger;
using Spring.Domain.Projections.BankAccountLedger.Reducers;

using Xunit;


namespace Spring.Domain.L0Tests.Projections.BankAccountLedger;

/// <summary>
///     Tests for <see cref="FundsWithdrawnLedgerReducer" />.
/// </summary>
[AllureParentSuite("Spring Domain")]
[AllureSuite("Projections")]
[AllureSubSuite("BankAccountLedger - FundsWithdrawn")]
public sealed class FundsWithdrawnLedgerReducerTests
{
    private readonly FundsWithdrawnLedgerReducer reducer = new();

    /// <summary>
    ///     Reducing FundsWithdrawn adds a withdrawal entry to the ledger.
    /// </summary>
    [Fact]
    [AllureFeature("Ledger Entries")]
    public void ReduceAddWithdrawalEntryToLedger()
    {
        // Arrange
        BankAccountLedgerProjection initial = new()
        {
            Entries = [],
            CurrentSequence = 0,
        };
        FundsWithdrawn evt = new() { Amount = 200m };

        // Act
        BankAccountLedgerProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Entries.Should().ContainSingle();
        result.Entries[0].EntryType.Should().Be(LedgerEntryType.Withdrawal);
        result.Entries[0].Amount.Should().Be(200m);
        result.Entries[0].Sequence.Should().Be(1);
    }

    /// <summary>
    ///     Reducing FundsWithdrawn increments the current sequence.
    /// </summary>
    [Fact]
    [AllureFeature("Sequencing")]
    public void ReduceIncrementsCurrentSequence()
    {
        // Arrange
        BankAccountLedgerProjection initial = new()
        {
            Entries = [],
            CurrentSequence = 10,
        };
        FundsWithdrawn evt = new() { Amount = 50m };

        // Act
        BankAccountLedgerProjection result = reducer.Apply(initial, evt);

        // Assert
        result.CurrentSequence.Should().Be(11);
        result.Entries[0].Sequence.Should().Be(11);
    }

    /// <summary>
    ///     New entries are prepended (most recent first).
    /// </summary>
    [Fact]
    [AllureFeature("Ordering")]
    public void ReducePrependsNewEntry()
    {
        // Arrange
        BankAccountLedgerProjection initial = new()
        {
            Entries = [new LedgerEntry { EntryType = LedgerEntryType.Deposit, Amount = 500m, Sequence = 1 }],
            CurrentSequence = 1,
        };
        FundsWithdrawn evt = new() { Amount = 100m };

        // Act
        BankAccountLedgerProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Entries.Should().HaveCount(2);
        result.Entries[0].EntryType.Should().Be(LedgerEntryType.Withdrawal);
        result.Entries[0].Amount.Should().Be(100m, "newest entry should be first");
        result.Entries[1].Amount.Should().Be(500m, "older entry should be second");
    }

    /// <summary>
    ///     Ledger entries are capped at MaxEntries (20).
    /// </summary>
    [Fact]
    [AllureFeature("Entry Limits")]
    public void ReduceCapsEntriesAtMaxEntries()
    {
        // Arrange - entries stored most-recent-first (descending sequence order)
        ImmutableArray<LedgerEntry> existingEntries = Enumerable.Range(1, 20)
            .Reverse()
            .Select(i => new LedgerEntry
            {
                EntryType = LedgerEntryType.Withdrawal,
                Amount = i * 5m,
                Sequence = i,
            })
            .ToImmutableArray();
        BankAccountLedgerProjection initial = new()
        {
            Entries = existingEntries,
            CurrentSequence = 20,
        };
        FundsWithdrawn evt = new() { Amount = 777m };

        // Act
        BankAccountLedgerProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Entries.Should().HaveCount(BankAccountLedgerProjection.MaxEntries);
        result.Entries[0].Amount.Should().Be(777m, "newest entry should be first");
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
        BankAccountLedgerProjection initial = new() { Entries = [], CurrentSequence = 0 };

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
        BankAccountLedgerProjection initial = new()
        {
            Entries = [],
            CurrentSequence = 0,
        };
        FundsWithdrawn evt = new() { Amount = 100m };

        // Act
        BankAccountLedgerProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Should().NotBeSameAs(initial);
    }

    /// <summary>
    ///     Withdrawal with zero amount still adds an entry.
    /// </summary>
    [Fact]
    [AllureFeature("Edge Cases")]
    public void ReduceWithZeroAmountAddsEntry()
    {
        // Arrange
        BankAccountLedgerProjection initial = new()
        {
            Entries = [],
            CurrentSequence = 0,
        };
        FundsWithdrawn evt = new() { Amount = 0m };

        // Act
        BankAccountLedgerProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Entries.Should().ContainSingle();
        result.Entries[0].Amount.Should().Be(0m);
    }

    /// <summary>
    ///     Mixed deposit and withdrawal entries maintain correct ordering.
    /// </summary>
    [Fact]
    [AllureFeature("Ordering")]
    public void MixedEntriesMaintainCorrectOrdering()
    {
        // Arrange
        BankAccountLedgerProjection initial = new()
        {
            Entries =
            [
                new LedgerEntry { EntryType = LedgerEntryType.Deposit, Amount = 1000m, Sequence = 2 },
                new LedgerEntry { EntryType = LedgerEntryType.Deposit, Amount = 500m, Sequence = 1 },
            ],
            CurrentSequence = 2,
        };
        FundsWithdrawn evt = new() { Amount = 150m };

        // Act
        BankAccountLedgerProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Entries.Should().HaveCount(3);
        result.Entries[0].EntryType.Should().Be(LedgerEntryType.Withdrawal);
        result.Entries[0].Sequence.Should().Be(3);
        result.Entries[1].EntryType.Should().Be(LedgerEntryType.Deposit);
        result.Entries[1].Sequence.Should().Be(2);
        result.Entries[2].EntryType.Should().Be(LedgerEntryType.Deposit);
        result.Entries[2].Sequence.Should().Be(1);
    }
}
