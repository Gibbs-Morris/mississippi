using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Aggregates.BankAccount.Handlers;
using Spring.Domain.Aggregates.BankAccount.Reducers;


namespace Spring.Domain.L0Tests.Fixtures;

/// <summary>
///     Pre-wired test fixture for BankAccount aggregate testing.
/// </summary>
/// <remarks>
///     <para>
///         This fixture eliminates boilerplate by pre-registering all handlers and reducers
///         for the BankAccount aggregate. Use the static factory methods for common scenarios.
///     </para>
/// </remarks>
public static class BankAccountFixture
{
    /// <summary>
    ///     Creates a scenario with an account at a specific balance for withdrawal testing.
    /// </summary>
    /// <param name="holderName">The account holder's name.</param>
    /// <param name="balance">The current balance.</param>
    /// <returns>A scenario ready for withdrawal commands.</returns>
    public static AggregateScenario<BankAccountAggregate> AccountWithBalance(
        string holderName,
        decimal balance
    ) =>
        CreateHarness()
            .WithInitialState(
                new()
                {
                    HolderName = holderName,
                    Balance = balance,
                    IsOpen = true,
                    DepositCount = 1,
                    WithdrawalCount = 0,
                })
            .CreateScenario();

    /// <summary>
    ///     Creates a scenario starting from a closed (default) account state.
    /// </summary>
    /// <returns>A scenario for testing account opening.</returns>
    public static AggregateScenario<BankAccountAggregate> ClosedAccount() => CreateHarness().CreateScenario();

    /// <summary>
    ///     Creates a fully-wired aggregate test harness with all BankAccount handlers and reducers.
    /// </summary>
    /// <returns>A configured <see cref="AggregateTestHarness{BankAccountAggregate}" />.</returns>
    public static AggregateTestHarness<BankAccountAggregate> CreateHarness() =>
        CommandHandlerTestExtensions.ForAggregate<BankAccountAggregate>()
            .WithHandler<OpenAccountHandler>()
            .WithHandler<DepositFundsHandler>()
            .WithHandler<WithdrawFundsHandler>()
            .WithReducer<AccountOpenedReducer>()
            .WithReducer<FundsDepositedReducer>()
            .WithReducer<FundsWithdrawnReducer>();

    /// <summary>
    ///     Creates a scenario starting from a freshly opened account.
    /// </summary>
    /// <param name="holderName">The account holder's name.</param>
    /// <param name="initialDeposit">The initial deposit amount.</param>
    /// <returns>A scenario ready for <c>When</c>/<c>Then</c> operations.</returns>
    public static AggregateScenario<BankAccountAggregate> OpenAccount(
        string holderName,
        decimal initialDeposit = 0m
    ) =>
        CreateHarness()
            .CreateScenario()
            .Given(
                new AccountOpened
                {
                    HolderName = holderName,
                    InitialDeposit = initialDeposit,
                });
}