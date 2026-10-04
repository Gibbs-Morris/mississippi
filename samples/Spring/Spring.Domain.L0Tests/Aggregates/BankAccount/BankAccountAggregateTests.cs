using System.Linq;

using Mississippi.DomainModeling.Abstractions;

using MississippiSamples.Spring.Domain.Aggregates.BankAccount;
using MississippiSamples.Spring.Domain.Aggregates.BankAccount.Commands;
using MississippiSamples.Spring.Domain.Aggregates.BankAccount.Events;
using MississippiSamples.Spring.Domain.Aggregates.BankAccount.Handlers;
using MississippiSamples.Spring.Domain.Aggregates.BankAccount.Reducers;

using Xunit.Sdk;


namespace MississippiSamples.Spring.Domain.L0Tests.Aggregates.BankAccount;

/// <summary>
///     Integration tests for the BankAccount aggregate using full command/event scenarios.
/// </summary>
/// <remarks>
///     These tests validate complete aggregate workflows using the unified
///     testing harness with Given/When/Then semantics.
/// </remarks>
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
                Assert.Equal(1550m, s.Balance);
                Assert.Equal(2, s.DepositCount);
                Assert.Equal(1, s.WithdrawalCount);
            });
    }

    /// <summary>
    ///     Depositing to an existing account should update balance.
    /// </summary>
    [Fact]
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
            .ThenEmits<FundsDeposited>(e => Assert.Equal(50m, e.Amount))
            .ThenState(s =>
            {
                Assert.Equal(150m, s.Balance);
                Assert.Equal(1, s.DepositCount);
            });
    }

    /// <summary>
    ///     Depositing to a non-existent account should fail.
    /// </summary>
    [Fact]
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
        Assert.Empty(scenario.EmittedEvents);
    }

    /// <summary>
    ///     Multiple transactions should maintain correct balance.
    /// </summary>
    [Fact]
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
                Assert.Equal(245m, s.Balance);
                Assert.Equal(3, s.DepositCount);
                Assert.Equal(1, s.WithdrawalCount);
            });
    }

    /// <summary>
    ///     Opening a new account should establish initial state.
    /// </summary>
    [Fact]
    public void OpenAccountEstablishesInitialState()
    {
        // Arrange & Act & Assert
        CreateHarness()
            .CreateScenario()
            .When(new OpenAccount("Alice", 500m))
            .ThenEmits<AccountOpened>(e =>
            {
                Assert.Equal("Alice", e.HolderName);
                Assert.Equal(500m, e.InitialDeposit);
            })
            .ThenState(s =>
            {
                Assert.True(s.IsOpen);
                Assert.Equal("Alice", s.HolderName);
                Assert.Equal(500m, s.Balance);
            });
    }

    /// <summary>
    ///     Opening an already open account should fail.
    /// </summary>
    [Fact]
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
        Assert.Empty(scenario.EmittedEvents);
    }

    /// <summary>
    ///     State after failed command should remain unchanged.
    /// </summary>
    [Fact]
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
        Assert.Equivalent(stateBefore, scenario.State, true);
    }

    /// <summary>
    ///     Assertions before a command should fail without invoking the event assertion.
    /// </summary>
    [Fact]
    public void ThenEmitsBeforeCommandDoesNotInvokeAssertion()
    {
        // Arrange
        AggregateScenario<BankAccountAggregate> scenario = CreateHarness().CreateScenario();
        bool wasAssertionInvoked = false;

        // Act
        XunitException exception = Assert.ThrowsAny<XunitException>(() =>
            scenario.ThenEmits<FundsDeposited>(_ => wasAssertionInvoked = true));

        // Assert
        Assert.False(wasAssertionInvoked);
        Assert.Contains("When() must be called before ThenEmits()", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     An emitted event should be passed to the assertion exactly once while preserving fluent chaining.
    /// </summary>
    [Fact]
    public void ThenEmitsInvokesAssertionOnceWithEmittedEvent()
    {
        // Arrange
        AggregateScenario<BankAccountAggregate> scenario = CreateHarness()
            .CreateScenario()
            .When(new OpenAccount("Alice", 500m));
        AccountOpened? receivedEvent = null;
        int assertionInvocationCount = 0;

        // Act
        AggregateScenario<BankAccountAggregate> returnedScenario = scenario.ThenEmits<AccountOpened>(evt =>
        {
            receivedEvent = evt;
            assertionInvocationCount++;
        });

        // Assert
        Assert.Equal(1, assertionInvocationCount);
        Assert.Same(scenario.EmittedEvents.Single(), receivedEvent);
        Assert.Equivalent(
            new AccountOpened
            {
                HolderName = "Alice",
                InitialDeposit = 500m,
            },
            receivedEvent,
            true);
        Assert.Same(scenario, returnedScenario);
    }

    /// <summary>
    ///     Missing events should throw an assertion failure without invoking the event assertion.
    /// </summary>
    [Fact]
    public void ThenEmitsMissingEventThrowsAssertionFailure()
    {
        // Arrange
        AggregateScenario<BankAccountAggregate> scenario = CreateHarness()
            .CreateScenario()
            .When(new OpenAccount("Alice", 500m));
        bool wasAssertionInvoked = false;

        // Act
        Action act = () => scenario.ThenEmits<FundsDeposited>(_ => wasAssertionInvoked = true);

        // Assert
        Assert.Contains(
            "Expected event of type FundsDeposited to be emitted",
            Assert.ThrowsAny<XunitException>(act).Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.False(wasAssertionInvoked);
    }

    /// <summary>
    ///     An emitted event should support fluent chaining without an optional assertion.
    /// </summary>
    [Fact]
    public void ThenEmitsWithoutAssertionReturnsScenario()
    {
        // Arrange
        AggregateScenario<BankAccountAggregate> scenario = CreateHarness()
            .CreateScenario()
            .When(new OpenAccount("Alice", 500m));

        // Act
        AggregateScenario<BankAccountAggregate> returnedScenario = scenario.ThenEmits<AccountOpened>();

        // Assert
        Assert.Same(scenario, returnedScenario);
    }

    /// <summary>
    ///     Withdrawing from an existing account should update balance.
    /// </summary>
    [Fact]
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
            .ThenEmits<FundsWithdrawn>(e => Assert.Equal(75m, e.Amount))
            .ThenState(s =>
            {
                Assert.Equal(125m, s.Balance);
                Assert.Equal(1, s.WithdrawalCount);
            });
    }

    /// <summary>
    ///     Withdrawing more than balance should fail.
    /// </summary>
    [Fact]
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
        Assert.Empty(scenario.EmittedEvents);
    }
}