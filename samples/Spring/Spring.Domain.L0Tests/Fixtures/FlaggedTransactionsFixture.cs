using System;
using System.Collections.Immutable;
using System.Linq;

using Spring.Domain.Aggregates.TransactionInvestigationQueue.Events;
using Spring.Domain.Projections.FlaggedTransactions;
using Spring.Domain.Projections.FlaggedTransactions.Reducers;


namespace Spring.Domain.L0Tests.Fixtures;

/// <summary>
///     Pre-wired test fixture for FlaggedTransactions projection testing.
/// </summary>
/// <remarks>
///     <para>
///         This fixture eliminates boilerplate by pre-registering all reducers
///         for the FlaggedTransactions projection.
///     </para>
///     <example>
///         <code>
///         // Quick projection testing
///         var result = FlaggedTransactionsFixture.Replay(
///             new TransactionFlagged { AccountId = "acc-1", Amount = 10000m, ... },
///             new TransactionFlagged { AccountId = "acc-2", Amount = 20000m, ... });
///         result.Entries.Should().HaveCount(2);
///
///         // Scenario testing
///         FlaggedTransactionsFixture.Empty()
///             .When(new TransactionFlagged { AccountId = "acc-1", Amount = 15000m, ... })
///             .ThenAssert(p => p.Entries.Should().ContainSingle());
///         </code>
///     </example>
/// </remarks>
public static class FlaggedTransactionsFixture
{
    /// <summary>
    ///     Creates a fully-wired projection test harness with all FlaggedTransactions reducers.
    /// </summary>
    /// <returns>A configured <see cref="ReducerTestHarness{FlaggedTransactionsProjection}" />.</returns>
    public static ReducerTestHarness<FlaggedTransactionsProjection> CreateHarness() =>
        ReducerTestExtensions.ForProjection<FlaggedTransactionsProjection>()
            .WithReducer<TransactionFlaggedProjectionReducer>();

    /// <summary>
    ///     Creates a scenario starting from an empty flagged transactions list.
    /// </summary>
    /// <returns>A scenario for testing flagged transaction events.</returns>
    public static ProjectionScenario<FlaggedTransactionsProjection> Empty() =>
        CreateHarness().CreateScenario();

    /// <summary>
    ///     Creates a scenario starting from a list with existing flagged transactions.
    /// </summary>
    /// <param name="entryCount">The number of existing flagged entries.</param>
    /// <returns>A scenario for testing additional flagged transactions.</returns>
    public static ProjectionScenario<FlaggedTransactionsProjection> WithEntries(int entryCount)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ImmutableArray<FlaggedTransaction> entries = Enumerable.Range(1, entryCount)
            .Select(i => new FlaggedTransaction
            {
                AccountId = $"account-{i}",
                Amount = i * 1000m,
                OriginalTimestamp = now.AddHours(-entryCount + i - 1),
                FlaggedTimestamp = now.AddHours(-entryCount + i),
                Sequence = entryCount - i + 1, // Most recent first
            })
            .ToImmutableArray();

        return CreateHarness()
            .WithInitialState(new FlaggedTransactionsProjection
            {
                CurrentSequence = entryCount,
                Entries = entries,
            })
            .CreateScenario();
    }

    /// <summary>
    ///     Quickly applies a sequence of events and returns the final projection.
    /// </summary>
    /// <param name="events">The events to apply in order.</param>
    /// <returns>The resulting projection state.</returns>
    public static FlaggedTransactionsProjection Replay(params object[] events) =>
        CreateHarness().ApplyEvents(events);
}
