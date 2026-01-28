using Spring.Domain.Aggregates.TransactionInvestigationQueue;
using Spring.Domain.Aggregates.TransactionInvestigationQueue.Handlers;
using Spring.Domain.Aggregates.TransactionInvestigationQueue.Reducers;


namespace Spring.Domain.L0Tests.Fixtures;

/// <summary>
///     Pre-wired test fixture for TransactionInvestigationQueue aggregate testing.
/// </summary>
/// <remarks>
///     <para>
///         This fixture eliminates boilerplate by pre-registering all handlers and reducers
///         for the TransactionInvestigationQueue aggregate.
///     </para>
/// </remarks>
public static class TransactionInvestigationQueueFixture
{
    /// <summary>
    ///     Creates a fully-wired aggregate test harness with all handlers and reducers.
    /// </summary>
    /// <returns>A configured <see cref="AggregateTestHarness{TransactionInvestigationQueueAggregate}" />.</returns>
    public static AggregateTestHarness<TransactionInvestigationQueueAggregate> CreateHarness() =>
        CommandHandlerTestExtensions.ForAggregate<TransactionInvestigationQueueAggregate>()
            .WithHandler<FlagTransactionHandler>()
            .WithReducer<TransactionFlaggedReducer>();

    /// <summary>
    ///     Creates a scenario starting from an empty queue (no flagged transactions).
    /// </summary>
    /// <returns>A scenario for testing transaction flagging.</returns>
    public static AggregateScenario<TransactionInvestigationQueueAggregate> EmptyQueue() =>
        CreateHarness().CreateScenario();

    /// <summary>
    ///     Creates a scenario starting from a queue with existing flagged transactions.
    /// </summary>
    /// <param name="flaggedCount">The number of previously flagged transactions.</param>
    /// <returns>A scenario for testing additional flagging.</returns>
    public static AggregateScenario<TransactionInvestigationQueueAggregate> WithFlaggedCount(
        int flaggedCount
    ) =>
        CreateHarness()
            .WithInitialState(
                new()
                {
                    TotalFlaggedCount = flaggedCount,
                })
            .CreateScenario();
}