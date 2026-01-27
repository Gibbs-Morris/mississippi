using System.Collections.Immutable;
using System.Linq;

using Allure.Xunit.Attributes;

using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Projections.BankAccountLedger;
using Spring.Domain.Projections.BankAccountLedger.Reducers;


namespace Spring.Domain.L0Tests.Projections.BankAccountLedger;

/// <summary>
///     Tests for <see cref="FundsDepositedLedgerReducer" />.
/// </summary>
[AllureParentSuite("Spring Domain")]
[AllureSuite("Projections")]
[AllureSubSuite("BankAccountLedger - FundsDeposited")]
public sealed class FundsDepositedLedgerReducerTests
{
    private readonly FundsDepositedLedgerReducer reducer = new();

    /// <summary>
    ///     Reducing FundsDeposited adds a deposit entry to the ledger.
    /// </summary>
    [Fact]
    [AllureFeature("Ledger Entries")]
    public void ReduceAddDepositEntryToLedger()
    {
        // Arrange
        BankAccountLedgerProjection initial = new()
        {
            Entries = [],
            CurrentSequence = 0,
        };
        FundsDeposited evt = new()
        {
            Amount = 500m,
        };

        // Act
        BankAccountLedgerProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Entries.Should().ContainSingle();
        result.Entries[0].EntryType.Should().Be(LedgerEntryType.Deposit);
        result.Entries[0].Amount.Should().Be(500m);
        result.Entries[0].Sequence.Should().Be(1);
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
                EntryType = LedgerEntryType.Deposit,
                Amount = i * 10m,
                Sequence = i,
            })
            .ToImmutableArray();
        BankAccountLedgerProjection initial = new()
        {
            Entries = existingEntries,
            CurrentSequence = 20,
        };
        FundsDeposited evt = new()
        {
            Amount = 999m,
        };

        // Act
        BankAccountLedgerProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Entries.Should().HaveCount(BankAccountLedgerProjection.MaxEntries);
        result.Entries[0].Amount.Should().Be(999m, "newest entry should be first");
        result.Entries[^1].Sequence.Should().Be(2, "oldest entry (seq 1) should be dropped");
    }

    /// <summary>
    ///     Reducing FundsDeposited increments the current sequence.
    /// </summary>
    [Fact]
    [AllureFeature("Sequencing")]
    public void ReduceIncrementsCurrentSequence()
    {
        // Arrange
        BankAccountLedgerProjection initial = new()
        {
            Entries = [],
            CurrentSequence = 5,
        };
        FundsDeposited evt = new()
        {
            Amount = 100m,
        };

        // Act
        BankAccountLedgerProjection result = reducer.Apply(initial, evt);

        // Assert
        result.CurrentSequence.Should().Be(6);
        result.Entries[0].Sequence.Should().Be(6);
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
            Entries =
            [
                new()
                {
                    EntryType = LedgerEntryType.Deposit,
                    Amount = 100m,
                    Sequence = 1,
                },
            ],
            CurrentSequence = 1,
        };
        FundsDeposited evt = new()
        {
            Amount = 200m,
        };

        // Act
        BankAccountLedgerProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Entries.Should().HaveCount(2);
        result.Entries[0].Amount.Should().Be(200m, "newest entry should be first");
        result.Entries[1].Amount.Should().Be(100m, "older entry should be second");
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
        FundsDeposited evt = new()
        {
            Amount = 100m,
        };

        // Act
        BankAccountLedgerProjection result = reducer.Apply(initial, evt);

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
        BankAccountLedgerProjection initial = new()
        {
            Entries = [],
            CurrentSequence = 0,
        };

        // Act
        Action act = () => reducer.Apply(initial, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    ///     Deposit with zero amount still adds an entry.
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
        FundsDeposited evt = new()
        {
            Amount = 0m,
        };

        // Act
        BankAccountLedgerProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Entries.Should().ContainSingle();
        result.Entries[0].Amount.Should().Be(0m);
    }
}