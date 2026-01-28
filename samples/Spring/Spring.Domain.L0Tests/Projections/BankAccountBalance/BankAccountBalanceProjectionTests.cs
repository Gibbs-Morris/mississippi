using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Projections.BankAccountBalance;
using Spring.Domain.Projections.BankAccountBalance.Reducers;


namespace Spring.Domain.L0Tests.Projections.BankAccountBalance;

/// <summary>
///     Integration tests for the BankAccountBalance projection using the full reducer chain.
///     Demonstrates realistic event replay scenarios.
/// </summary>
public sealed class BankAccountBalanceProjectionTests
{
    /// <summary>
    ///     Creates a test harness configured with all BankAccountBalance reducers.
    /// </summary>
    private static ReducerTestHarness<BankAccountBalanceProjection> CreateHarness() =>
        ReducerTestExtensions.ForProjection<BankAccountBalanceProjection>()
            .WithReducer<AccountOpenedBalanceReducer>()
            .WithReducer<FundsDepositedBalanceReducer>()
            .WithReducer<FundsWithdrawnBalanceReducer>();

    /// <summary>
    ///     Verifies that using ApplyEvents convenience method works correctly.
    /// </summary>
    [Fact]
    public void ApplyEventsWithMultipleEventsReturnsCorrectProjection()
    {
        // Arrange
        ReducerTestHarness<BankAccountBalanceProjection> harness = CreateHarness();

        // Act
        BankAccountBalanceProjection result = harness.ApplyEvents(
            new AccountOpened
            {
                HolderName = "Quick Test",
                InitialDeposit = 200.00m,
            },
            new FundsDeposited
            {
                Amount = 100.00m,
            },
            new FundsWithdrawn
            {
                Amount = 50.00m,
            });

        // Assert
        result.Balance.Should().Be(250.00m);
        result.HolderName.Should().Be("Quick Test");
    }

    /// <summary>
    ///     Verifies that the projection tracks the event history correctly.
    /// </summary>
    [Fact]
    public void ScenarioAppliedEventsTracksEventHistory()
    {
        // Arrange & Act
        ProjectionScenario<BankAccountBalanceProjection> scenario = CreateHarness()
            .CreateScenario()
            .When(
                new AccountOpened
                {
                    HolderName = "History",
                    InitialDeposit = 100.00m,
                })
            .When(
                new FundsDeposited
                {
                    Amount = 50.00m,
                });

        // Assert
        scenario.AppliedEvents.Should().HaveCount(2);
        scenario.AppliedEvents[0].Should().BeOfType<AccountOpened>();
        scenario.AppliedEvents[1].Should().BeOfType<FundsDeposited>();
    }

    /// <summary>
    ///     Verifies a full lifecycle scenario with deposits and withdrawals.
    /// </summary>
    [Fact]
    public void ScenarioFullLifecycleBalanceIsCorrect()
    {
        CreateHarness()
            .CreateScenario()
            .When(
                new AccountOpened
                {
                    HolderName = "Full Lifecycle",
                    InitialDeposit = 1000.00m,
                })
            .When(
                new FundsDeposited
                {
                    Amount = 500.00m,
                })
            .When(
                new FundsWithdrawn
                {
                    Amount = 200.00m,
                })
            .When(
                new FundsDeposited
                {
                    Amount = 100.00m,
                })
            .When(
                new FundsWithdrawn
                {
                    Amount = 50.00m,
                })
            .ThenShouldBe(
                new()
                {
                    HolderName = "Full Lifecycle",
                    Balance = 1350.00m, // 1000 + 500 - 200 + 100 - 50
                    IsOpen = true,
                });
    }

    /// <summary>
    ///     Verifies that Given/When/Then pattern works for testing state transitions.
    /// </summary>
    [Fact]
    public void ScenarioGivenPriorEventsWhenNewEventThenCorrectState()
    {
        // Given an account was opened and had activity
        // When a new deposit occurs
        // Then the balance should reflect all events
        CreateHarness()
            .CreateScenario()
            .Given(
                new AccountOpened
                {
                    HolderName = "Prior State",
                    InitialDeposit = 100.00m,
                },
                new FundsDeposited
                {
                    Amount = 50.00m,
                })
            .When(
                new FundsDeposited
                {
                    Amount = 25.00m,
                })
            .ThenAssert(projection => { projection.Balance.Should().Be(175.00m); });
    }

    /// <summary>
    ///     Verifies that opening an account and making deposits results in correct balance.
    /// </summary>
    [Fact]
    public void ScenarioOpenAccountAndDepositBalanceReflectsTotal()
    {
        // Arrange & Act & Assert
        CreateHarness()
            .CreateScenario()
            .When(
                new AccountOpened
                {
                    HolderName = "John Doe",
                    InitialDeposit = 100.00m,
                })
            .When(
                new FundsDeposited
                {
                    Amount = 50.00m,
                })
            .When(
                new FundsDeposited
                {
                    Amount = 25.00m,
                })
            .ThenAssert(projection =>
            {
                projection.HolderName.Should().Be("John Doe");
                projection.Balance.Should().Be(175.00m);
                projection.IsOpen.Should().BeTrue();
            });
    }

    /// <summary>
    ///     Verifies that opening an account and making withdrawals results in correct balance.
    /// </summary>
    [Fact]
    public void ScenarioOpenAccountAndWithdrawBalanceReflectsTotal()
    {
        CreateHarness()
            .CreateScenario()
            .When(
                new AccountOpened
                {
                    HolderName = "Jane Doe",
                    InitialDeposit = 500.00m,
                })
            .When(
                new FundsWithdrawn
                {
                    Amount = 100.00m,
                })
            .When(
                new FundsWithdrawn
                {
                    Amount = 50.00m,
                })
            .ThenAssert(projection => { projection.Balance.Should().Be(350.00m); });
    }

    /// <summary>
    ///     Verifies that predicate-based assertions work correctly.
    /// </summary>
    [Fact]
    public void ScenarioThenShouldSatisfyValidatesPredicate()
    {
        CreateHarness()
            .CreateScenario()
            .When(
                new AccountOpened
                {
                    HolderName = "Predicate Test",
                    InitialDeposit = 100.00m,
                })
            .ThenShouldSatisfy(
                p => p.IsOpen && (p.Balance > 0),
                "because account should be open with positive balance");
    }
}