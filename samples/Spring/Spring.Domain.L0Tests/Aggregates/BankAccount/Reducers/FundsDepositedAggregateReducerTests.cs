using System;

using Allure.Xunit.Attributes;

using FluentAssertions;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Aggregates.BankAccount.Reducers;

using Xunit;


namespace Spring.Domain.L0Tests.Aggregates.BankAccount.Reducers;

/// <summary>
///     Tests for <see cref="FundsDepositedReducer" /> for the aggregate.
/// </summary>
[AllureParentSuite("Spring Domain")]
[AllureSuite("Aggregates")]
[AllureSubSuite("FundsDepositedReducer (Aggregate)")]
public sealed class FundsDepositedAggregateReducerTests
{
    private readonly FundsDepositedReducer reducer = new();

    private static BankAccountAggregate OpenAccount => new()
    {
        IsOpen = true,
        HolderName = "Test User",
        Balance = 100m,
        DepositCount = 0,
        WithdrawalCount = 0,
    };

    /// <summary>
    ///     Reducing FundsDeposited should increase balance by amount.
    /// </summary>
    [Fact]
    [AllureFeature("Balance Updates")]
    public void FundsDepositedIncreasesBalance()
    {
        // Arrange
        FundsDeposited @event = new() { Amount = 50m };

        // Act
        BankAccountAggregate result = reducer.Apply(OpenAccount, @event);

        // Assert
        result.Balance.Should().Be(150m);
    }

    /// <summary>
    ///     Reducing FundsDeposited should increment deposit count.
    /// </summary>
    [Fact]
    [AllureFeature("Counters")]
    public void FundsDepositedIncrementsDepositCount()
    {
        // Arrange
        FundsDeposited @event = new() { Amount = 50m };

        // Act
        BankAccountAggregate result = reducer.Apply(OpenAccount, @event);

        // Assert
        result.DepositCount.Should().Be(1);
    }

    /// <summary>
    ///     Reducing FundsDeposited should not change other properties.
    /// </summary>
    [Fact]
    [AllureFeature("State Isolation")]
    public void FundsDepositedPreservesOtherProperties()
    {
        // Arrange
        FundsDeposited @event = new() { Amount = 50m };

        // Act
        BankAccountAggregate result = reducer.Apply(OpenAccount, @event);

        // Assert
        result.IsOpen.Should().Be(OpenAccount.IsOpen);
        result.HolderName.Should().Be(OpenAccount.HolderName);
        result.WithdrawalCount.Should().Be(OpenAccount.WithdrawalCount);
    }

    /// <summary>
    ///     Reducing FundsDeposited with null event should throw.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void FundsDepositedWithNullEventThrows()
    {
        // Act
        Action act = () => reducer.Apply(OpenAccount, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    ///     Reducer should produce expected complete state.
    /// </summary>
    [Fact]
    [AllureFeature("Balance Updates")]
    public void FundsDepositedProducesExpectedState()
    {
        // Arrange
        FundsDeposited @event = new() { Amount = 75m };

        // Act & Assert
        reducer.ShouldProduce(
            OpenAccount,
            @event,
            new BankAccountAggregate
            {
                IsOpen = true,
                HolderName = "Test User",
                Balance = 175m,
                DepositCount = 1,
                WithdrawalCount = 0,
            });
    }
}
