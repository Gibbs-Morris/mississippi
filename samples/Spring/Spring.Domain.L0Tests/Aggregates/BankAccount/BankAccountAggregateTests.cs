using System.Linq;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Aggregates.BankAccount.Handlers;
using Spring.Domain.Aggregates.BankAccount.Reducers;


namespace Spring.Domain.L0Tests.Aggregates.BankAccount;

/// <summary>
///     Integration tests for the BankAccount aggregate using full command/event scenarios.
/// </summary>
/// <remarks>
///     These tests validate complete aggregate workflows using the unified
///     testing harness with Given/When/Then semantics.
/// </remarks>
[AllureParentSuite("Spring Domain")]
[AllureSuite("Aggregates")]
[AllureSubSuite("BankAccountAggregate Scenarios")]
public sealed class BankAccountAggregateTests
{
    /// <summary>
    ///     Creates a fully configured harness with all handlers and reducers.
    /// </summary>
    private static AggregateTestHarness<BankAccountAggregate> CreateHarness() =>
        CommandHandlerTestExtensions.ForAggregate<BankAccountAggregate>()
            .WithHandler<OpenAccountHandler>()
            .WithHandler<DepositFundsHandler>()
            .WithHandler<WithdrawFundsHandler>()
            .WithReducer<AccountOpenedReducer>()
            .WithReducer<FundsDepositedReducer>()
            .WithReducer<FundsWithdrawnReducer>();

    /// <summary>
    ///     Complete account lifecycle from open through transactions.
    /// </summary>
    [Fact]
    [AllureFeature("Account Lifecycle")]
    public void CompleteAccountLifecycleScenario()
    {
        // This test demonstrates a full lifecycle scenario
        AggregateTestHarness<BankAccountAggregate> harness = CreateHarness();

        // Open account
        AggregateScenario<BankAccountAggregate> scenario = harness.CreateScenario()
            .When(new OpenAccount("Ivy", 1000m))
            .ThenSucceeds();

        // Make deposits
        scenario = harness.CreateScenario()
            .Given(scenario.AllAppliedEvents.ToArray())
            .When(
                new DepositFunds
                {
                    Amount = 500m,
                })
            .ThenSucceeds();
        scenario = harness.CreateScenario()
            .Given(scenario.AllAppliedEvents.ToArray())
            .When(
                new DepositFunds
                {
                    Amount = 250m,
                })
            .ThenSucceeds();

        // Make withdrawal
        harness.CreateScenario()
            .Given(scenario.AllAppliedEvents.ToArray())
            .When(
                new WithdrawFunds
                {
                    Amount = 200m,
                })
            .ThenState(s =>
            {
                // 1000 + 500 + 250 - 200 = 1550
                s.Balance.Should().Be(1550m);
                s.DepositCount.Should().Be(2);
                s.WithdrawalCount.Should().Be(1);
            });
    }

    /// <summary>
    ///     Depositing to an existing account should update balance.
    /// </summary>
    [Fact]
    [AllureFeature("Transactions")]
    public void DepositToExistingAccountUpdatesBalance()
    {
        // Arrange & Act & Assert
        CreateHarness()
            .CreateScenario()
            .Given(
                new AccountOpened
                {
                    HolderName = "Bob",
                    InitialDeposit = 100m,
                })
            .When(
                new DepositFunds
                {
                    Amount = 50m,
                })
            .ThenEmits<FundsDeposited>(e => e.Amount.Should().Be(50m))
            .ThenState(s =>
            {
                s.Balance.Should().Be(150m);
                s.DepositCount.Should().Be(1);
            });
    }

    /// <summary>
    ///     Depositing to a non-existent account should fail.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void DepositToNonExistentAccountFails()
    {
        // Arrange & Act & Assert
        // ThenFails validates the command failed with correct error code
        // With OperationResult pattern, failures don't emit events - error is in result
        AggregateScenario<BankAccountAggregate> scenario = CreateHarness()
            .CreateScenario()
            .When(
                new DepositFunds
                {
                    Amount = 50m,
                })
            .ThenFails(AggregateErrorCodes.InvalidState);
        scenario.EmittedEvents.Should().BeEmpty("failures don't emit events with OperationResult");
    }

    /// <summary>
    ///     Multiple transactions should maintain correct balance.
    /// </summary>
    [Fact]
    [AllureFeature("Transactions")]
    public void MultipleTransactionsMaintainCorrectBalance()
    {
        // Arrange
        AggregateScenario<BankAccountAggregate> scenario = CreateHarness()
            .CreateScenario()
            .Given(
                new AccountOpened
                {
                    HolderName = "Dave",
                    InitialDeposit = 100m,
                },
                new FundsDeposited
                {
                    Amount = 50m,
                },
                new FundsDeposited
                {
                    Amount = 25m,
                },
                new FundsWithdrawn
                {
                    Amount = 30m,
                });

        // Act & Assert
        scenario.When(
                new DepositFunds
                {
                    Amount = 100m,
                })
            .ThenState(s =>
            {
                // 100 + 50 + 25 - 30 + 100 = 245
                s.Balance.Should().Be(245m);
                s.DepositCount.Should().Be(3);
                s.WithdrawalCount.Should().Be(1);
            });
    }

    /// <summary>
    ///     Opening a new account should establish initial state.
    /// </summary>
    [Fact]
    [AllureFeature("Account Lifecycle")]
    public void OpenAccountEstablishesInitialState()
    {
        // Arrange & Act & Assert
        CreateHarness()
            .CreateScenario()
            .When(new OpenAccount("Alice", 500m))
            .ThenEmits<AccountOpened>(e =>
            {
                e.HolderName.Should().Be("Alice");
                e.InitialDeposit.Should().Be(500m);
            })
            .ThenState(s =>
            {
                s.IsOpen.Should().BeTrue();
                s.HolderName.Should().Be("Alice");
                s.Balance.Should().Be(500m);
            });
    }

    /// <summary>
    ///     Opening an already open account should fail.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void OpenAlreadyOpenAccountFails()
    {
        // Arrange & Act & Assert
        // ThenFails validates the command failed with correct error code
        // With OperationResult pattern, failures don't emit events - error is in result
        AggregateScenario<BankAccountAggregate> scenario = CreateHarness()
            .CreateScenario()
            .Given(
                new AccountOpened
                {
                    HolderName = "Frank",
                    InitialDeposit = 100m,
                })
            .When(new OpenAccount("Grace", 200m))
            .ThenFails(AggregateErrorCodes.AlreadyExists);
        scenario.EmittedEvents.Should().BeEmpty("failures don't emit events with OperationResult");
    }

    /// <summary>
    ///     State after failed command should remain unchanged.
    /// </summary>
    [Fact]
    [AllureFeature("State Isolation")]
    public void StateAfterFailedCommandRemainsUnchanged()
    {
        // Arrange
        AggregateScenario<BankAccountAggregate> scenario = CreateHarness()
            .CreateScenario()
            .Given(
                new AccountOpened
                {
                    HolderName = "Hank",
                    InitialDeposit = 100m,
                });
        BankAccountAggregate stateBefore = scenario.State;

        // Act - attempt invalid withdrawal
        scenario.When(
            new WithdrawFunds
            {
                Amount = 200m,
            });

        // Assert - state should not have changed
        scenario.State.Should().BeEquivalentTo(stateBefore);
    }

    /// <summary>
    ///     Withdrawing from an existing account should update balance.
    /// </summary>
    [Fact]
    [AllureFeature("Transactions")]
    public void WithdrawFromExistingAccountUpdatesBalance()
    {
        // Arrange & Act & Assert
        CreateHarness()
            .CreateScenario()
            .Given(
                new AccountOpened
                {
                    HolderName = "Carol",
                    InitialDeposit = 200m,
                })
            .When(
                new WithdrawFunds
                {
                    Amount = 75m,
                })
            .ThenEmits<FundsWithdrawn>(e => e.Amount.Should().Be(75m))
            .ThenState(s =>
            {
                s.Balance.Should().Be(125m);
                s.WithdrawalCount.Should().Be(1);
            });
    }

    /// <summary>
    ///     Withdrawing more than balance should fail.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void WithdrawMoreThanBalanceFails()
    {
        // Arrange & Act & Assert
        // ThenFails validates the command failed with correct error code/message
        // With OperationResult pattern, failures don't emit events - error is in result
        AggregateScenario<BankAccountAggregate> scenario = CreateHarness()
            .CreateScenario()
            .Given(
                new AccountOpened
                {
                    HolderName = "Eve",
                    InitialDeposit = 50m,
                })
            .When(
                new WithdrawFunds
                {
                    Amount = 100m,
                })
            .ThenFails(AggregateErrorCodes.InvalidCommand, "Insufficient funds");
        scenario.EmittedEvents.Should().BeEmpty("failures don't emit events with OperationResult");
    }
}