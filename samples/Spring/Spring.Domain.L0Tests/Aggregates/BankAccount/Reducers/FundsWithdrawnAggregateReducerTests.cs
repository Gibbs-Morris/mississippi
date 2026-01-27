using Allure.Xunit.Attributes;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Aggregates.BankAccount.Reducers;


namespace Spring.Domain.L0Tests.Aggregates.BankAccount.Reducers;

/// <summary>
///     Tests for <see cref="FundsWithdrawnReducer" /> for the aggregate.
/// </summary>
[AllureParentSuite("Spring Domain")]
[AllureSuite("Aggregates")]
[AllureSubSuite("FundsWithdrawnReducer (Aggregate)")]
public sealed class FundsWithdrawnAggregateReducerTests
{
    private readonly FundsWithdrawnReducer reducer = new();

    private static BankAccountAggregate OpenAccount =>
        new()
        {
            IsOpen = true,
            HolderName = "Test User",
            Balance = 100m,
            DepositCount = 1,
            WithdrawalCount = 0,
        };

    /// <summary>
    ///     Reducing FundsWithdrawn should decrease balance by amount.
    /// </summary>
    [Fact]
    [AllureFeature("Balance Updates")]
    public void FundsWithdrawnDecreasesBalance()
    {
        // Arrange
        FundsWithdrawn @event = new()
        {
            Amount = 30m,
        };

        // Act
        BankAccountAggregate result = reducer.Apply(OpenAccount, @event);

        // Assert
        result.Balance.Should().Be(70m);
    }

    /// <summary>
    ///     Reducing FundsWithdrawn should increment withdrawal count.
    /// </summary>
    [Fact]
    [AllureFeature("Counters")]
    public void FundsWithdrawnIncrementsWithdrawalCount()
    {
        // Arrange
        FundsWithdrawn @event = new()
        {
            Amount = 30m,
        };

        // Act
        BankAccountAggregate result = reducer.Apply(OpenAccount, @event);

        // Assert
        result.WithdrawalCount.Should().Be(1);
    }

    /// <summary>
    ///     Reducing FundsWithdrawn should not change other properties.
    /// </summary>
    [Fact]
    [AllureFeature("State Isolation")]
    public void FundsWithdrawnPreservesOtherProperties()
    {
        // Arrange
        FundsWithdrawn @event = new()
        {
            Amount = 30m,
        };

        // Act
        BankAccountAggregate result = reducer.Apply(OpenAccount, @event);

        // Assert
        result.IsOpen.Should().Be(OpenAccount.IsOpen);
        result.HolderName.Should().Be(OpenAccount.HolderName);
        result.DepositCount.Should().Be(OpenAccount.DepositCount);
    }

    /// <summary>
    ///     Reducer should produce expected complete state.
    /// </summary>
    [Fact]
    [AllureFeature("Balance Updates")]
    public void FundsWithdrawnProducesExpectedState()
    {
        // Arrange
        FundsWithdrawn @event = new()
        {
            Amount = 25m,
        };

        // Act & Assert
        reducer.ShouldProduce(
            OpenAccount,
            @event,
            new()
            {
                IsOpen = true,
                HolderName = "Test User",
                Balance = 75m,
                DepositCount = 1,
                WithdrawalCount = 1,
            });
    }

    /// <summary>
    ///     Reducing FundsWithdrawn with null event should throw.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void FundsWithdrawnWithNullEventThrows()
    {
        // Act
        Action act = () => reducer.Apply(OpenAccount, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}