using System.Collections.Immutable;
using System.Linq;

using Spring.Domain.Projections.BankAccountLedger;
using Spring.Domain.Projections.BankAccountLedger.Reducers;


namespace Spring.Domain.L0Tests.Fixtures;

/// <summary>
///     Pre-wired test fixture for BankAccountLedger projection testing.
/// </summary>
/// <remarks>
///     <para>
///         This fixture eliminates boilerplate by pre-registering all reducers
///         for the BankAccountLedger projection.
///     </para>
///     <example>
///         <code>
///         // Quick projection testing
///         var result = BankAccountLedgerFixture.Replay(
///             new FundsDeposited { Amount = 100m },
///             new FundsWithdrawn { Amount = 50m });
///         result.Entries.Should().HaveCount(2);
///
///         // Scenario testing
///         BankAccountLedgerFixture.Empty()
///             .When(new FundsDeposited { Amount = 100m })
///             .ThenAssert(p => p.Entries.Should().ContainSingle());
///         </code>
///     </example>
/// </remarks>
public static class BankAccountLedgerFixture
{
    /// <summary>
    ///     Creates a fully-wired projection test harness with all BankAccountLedger reducers.
    /// </summary>
    /// <returns>A configured <see cref="ReducerTestHarness{BankAccountLedgerProjection}" />.</returns>
    public static ReducerTestHarness<BankAccountLedgerProjection> CreateHarness() =>
        ReducerTestExtensions.ForProjection<BankAccountLedgerProjection>()
            .WithReducer<FundsDepositedLedgerReducer>()
            .WithReducer<FundsWithdrawnLedgerReducer>();

    /// <summary>
    ///     Creates a scenario starting from an empty ledger.
    /// </summary>
    /// <returns>A scenario for testing ledger operations.</returns>
    public static ProjectionScenario<BankAccountLedgerProjection> Empty() => CreateHarness().CreateScenario();

    /// <summary>
    ///     Quickly applies a sequence of events and returns the final projection.
    /// </summary>
    /// <param name="events">The events to apply in order.</param>
    /// <returns>The resulting projection state.</returns>
    public static BankAccountLedgerProjection Replay(
        params object[] events
    ) =>
        CreateHarness().ApplyEvents(events);

    /// <summary>
    ///     Creates a scenario starting from a ledger with existing entries.
    /// </summary>
    /// <param name="entryCount">The number of existing entries.</param>
    /// <returns>A scenario for testing additional entries.</returns>
    public static ProjectionScenario<BankAccountLedgerProjection> WithEntries(
        int entryCount
    )
    {
        ImmutableArray<LedgerEntry> entries = Enumerable.Range(1, entryCount)
            .Select(i => new LedgerEntry
            {
                EntryType = (i % 2) == 0 ? LedgerEntryType.Withdrawal : LedgerEntryType.Deposit,
                Amount = i * 10m,
                Sequence = (entryCount - i) + 1, // Most recent first
            })
            .ToImmutableArray();
        return CreateHarness()
            .WithInitialState(
                new()
                {
                    CurrentSequence = entryCount,
                    Entries = entries,
                })
            .CreateScenario();
    }
}