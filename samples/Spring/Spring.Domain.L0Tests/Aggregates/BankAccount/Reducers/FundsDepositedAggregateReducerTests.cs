using MississippiSamples.Spring.Domain.Aggregates.BankAccount;
using MississippiSamples.Spring.Domain.Aggregates.BankAccount.Events;
using MississippiSamples.Spring.Domain.Aggregates.BankAccount.Reducers;


namespace MississippiSamples.Spring.Domain.L0Tests.Aggregates.BankAccount.Reducers;

/// <summary>
///     Tests for <see cref="FundsDepositedReducer" /> for the aggregate.
/// </summary>
public sealed class FundsDepositedAggregateReducerTests
{
    private readonly FundsDepositedReducer reducer = new();

    private static BankAccountAggregate OpenAccount =>
        new()
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
    public void FundsDepositedIncreasesBalance()
    {
        // Arrange
        FundsDeposited @event = new()
        {
            Amount = 50m,
        };

        // Act
        BankAccountAggregate result = reducer.Apply(OpenAccount, @event);

        // Assert
        Assert.Equal(150m, result.Balance);
    }

    /// <summary>
    ///     Reducing FundsDeposited should increment deposit count.
    /// </summary>
    [Fact]
    public void FundsDepositedIncrementsDepositCount()
    {
        // Arrange
        FundsDeposited @event = new()
        {
            Amount = 50m,
        };

        // Act
        BankAccountAggregate result = reducer.Apply(OpenAccount, @event);

        // Assert
        Assert.Equal(1, result.DepositCount);
    }

    /// <summary>
    ///     Reducing FundsDeposited should not change other properties.
    /// </summary>
    [Fact]
    public void FundsDepositedPreservesOtherProperties()
    {
        // Arrange
        FundsDeposited @event = new()
        {
            Amount = 50m,
        };

        // Act
        BankAccountAggregate result = reducer.Apply(OpenAccount, @event);

        // Assert
        Assert.Equal(OpenAccount.IsOpen, result.IsOpen);
        Assert.Equal(OpenAccount.HolderName, result.HolderName);
        Assert.Equal(OpenAccount.WithdrawalCount, result.WithdrawalCount);
    }

    /// <summary>
    ///     Reducer should produce expected complete state.
    /// </summary>
    [Fact]
    public void FundsDepositedProducesExpectedState()
    {
        // Arrange
        FundsDeposited @event = new()
        {
            Amount = 75m,
        };

        // Act & Assert
        reducer.ShouldProduce(
            OpenAccount,
            @event,
            new()
            {
                IsOpen = true,
                HolderName = "Test User",
                Balance = 175m,
                DepositCount = 1,
                WithdrawalCount = 0,
            });
    }

    /// <summary>
    ///     Reducing FundsDeposited with null event should throw.
    /// </summary>
    [Fact]
    public void FundsDepositedWithNullEventThrows()
    {
        // Act
        Action act = () => reducer.Apply(OpenAccount, null!);

        // Assert
        Assert.ThrowsAny<ArgumentNullException>(act);
    }
}