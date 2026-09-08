using System.Collections.Immutable;
using System.Linq;

using MississippiSamples.Spring.Domain.Aggregates.BankAccount.Events;
using MississippiSamples.Spring.Domain.Projections.BankAccountLedger;
using MississippiSamples.Spring.Domain.Projections.BankAccountLedger.Reducers;


namespace MississippiSamples.Spring.Domain.L0Tests.Projections.BankAccountLedger;

/// <summary>
///     Tests for <see cref="FundsDepositedLedgerReducer" />.
/// </summary>
public sealed class FundsDepositedLedgerReducerTests
{
    private readonly FundsDepositedLedgerReducer reducer = new();

    /// <summary>
    ///     Reducing FundsDeposited adds a deposit entry to the ledger.
    /// </summary>
    [Fact]
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
        Assert.Single(result.Entries);
        Assert.Equal(LedgerEntryType.Deposit, result.Entries[0].EntryType);
        Assert.Equal(500m, result.Entries[0].Amount);
        Assert.Equal(1, result.Entries[0].Sequence);
    }

    /// <summary>
    ///     Ledger entries are capped at MaxEntries (20).
    /// </summary>
    [Fact]
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
        Assert.Equal(BankAccountLedgerProjection.MaxEntries, result.Entries.Length);
        Assert.Equal(999m, result.Entries[0].Amount);
        Assert.Equal(2, result.Entries[^1].Sequence);
    }

    /// <summary>
    ///     Reducing FundsDeposited increments the current sequence.
    /// </summary>
    [Fact]
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
        Assert.Equal(6, result.CurrentSequence);
        Assert.Equal(6, result.Entries[0].Sequence);
    }

    /// <summary>
    ///     New entries are prepended (most recent first).
    /// </summary>
    [Fact]
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
        Assert.Equal(2, result.Entries.Length);
        Assert.Equal(200m, result.Entries[0].Amount);
        Assert.Equal(100m, result.Entries[1].Amount);
    }

    /// <summary>
    ///     Reducing returns a new projection instance (immutability check).
    /// </summary>
    [Fact]
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
        Assert.NotSame(initial, result);
    }

    /// <summary>
    ///     Reducing with null event throws ArgumentNullException.
    /// </summary>
    [Fact]
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
        Assert.ThrowsAny<ArgumentNullException>(act);
    }

    /// <summary>
    ///     Deposit with zero amount still adds an entry.
    /// </summary>
    [Fact]
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
        Assert.Single(result.Entries);
        Assert.Equal(0m, result.Entries[0].Amount);
    }
}