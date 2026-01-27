using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Projections.BankAccountBalance;
using Spring.Domain.Projections.BankAccountBalance.Reducers;


namespace Spring.Domain.L0Tests.Fixtures;

/// <summary>
///     Pre-wired test fixture for BankAccountBalance projection testing.
/// </summary>
/// <remarks>
///     <para>
///         This fixture eliminates boilerplate by pre-registering all reducers
///         for the BankAccountBalance projection. Use the static factory methods for common scenarios.
///     </para>
///     <example>
///         <code>
///         // Quick projection testing
///         BankAccountBalanceFixture.OpenAccount("John", 100m)
///             .When(new FundsDeposited { Amount = 50m })
///             .ThenAssert(p => p.Balance.Should().Be(150m));
///
///         // Replay multiple events
///         BankAccountBalanceFixture.CreateHarness()
///             .ApplyEvents(
///                 new AccountOpened { HolderName = "Test", InitialDeposit = 100m },
///                 new FundsDeposited { Amount = 50m },
///                 new FundsWithdrawn { Amount = 25m })
///             .Balance.Should().Be(125m);
///         </code>
///     </example>
/// </remarks>
public static class BankAccountBalanceFixture
{
    /// <summary>
    ///     Creates a fully-wired projection test harness with all BankAccountBalance reducers.
    /// </summary>
    /// <returns>A configured <see cref="ReducerTestHarness{BankAccountBalanceProjection}" />.</returns>
    public static ReducerTestHarness<BankAccountBalanceProjection> CreateHarness() =>
        ReducerTestExtensions.ForProjection<BankAccountBalanceProjection>()
            .WithReducer<AccountOpenedBalanceReducer>()
            .WithReducer<FundsDepositedBalanceReducer>()
            .WithReducer<FundsWithdrawnBalanceReducer>();

    /// <summary>
    ///     Creates a scenario starting from a freshly opened account.
    /// </summary>
    /// <param name="holderName">The account holder's name.</param>
    /// <param name="initialDeposit">The initial deposit amount.</param>
    /// <returns>A scenario ready for <c>When</c>/<c>ThenAssert</c> operations.</returns>
    public static ProjectionScenario<BankAccountBalanceProjection> OpenAccount(
        string holderName,
        decimal initialDeposit = 0m
    ) =>
        CreateHarness()
            .CreateScenario()
            .Given(new AccountOpened { HolderName = holderName, InitialDeposit = initialDeposit });

    /// <summary>
    ///     Creates a scenario starting from an empty projection.
    /// </summary>
    /// <returns>A scenario for testing account opening events.</returns>
    public static ProjectionScenario<BankAccountBalanceProjection> Empty() =>
        CreateHarness().CreateScenario();

    /// <summary>
    ///     Quickly applies a sequence of events and returns the final projection.
    /// </summary>
    /// <param name="events">The events to apply in order.</param>
    /// <returns>The resulting projection state.</returns>
    public static BankAccountBalanceProjection Replay(params object[] events) =>
        CreateHarness().ApplyEvents(events);
}
