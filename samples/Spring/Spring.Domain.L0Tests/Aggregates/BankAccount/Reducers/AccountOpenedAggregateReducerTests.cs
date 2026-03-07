using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Aggregates.BankAccount.Reducers;


namespace Spring.Domain.L0Tests.Aggregates.BankAccount.Reducers;

/// <summary>
///     Tests for <see cref="AccountOpenedReducer" /> for the aggregate.
/// </summary>
public sealed class AccountOpenedAggregateReducerTests
{
    private readonly AccountOpenedReducer reducer = new();

    /// <summary>
    ///     Reducer should produce expected complete state.
    /// </summary>
    [Fact]
    public void AccountOpenedProducesExpectedState()
    {
        // Arrange
        AccountOpened @event = new()
        {
            HolderName = "Jane",
            InitialDeposit = 500m,
        };

        // Act & Assert
        reducer.ShouldProduce(
            new(),
            @event,
            new()
            {
                IsOpen = true,
                HolderName = "Jane",
                Balance = 500m,
                DepositCount = 0,
                WithdrawalCount = 0,
            });
    }

    /// <summary>
    ///     Reducing AccountOpened should set Balance from InitialDeposit.
    /// </summary>
    [Fact]
    public void AccountOpenedSetsBalanceFromInitialDeposit()
    {
        // Arrange
        AccountOpened @event = new()
        {
            HolderName = "Test",
            InitialDeposit = 250.50m,
        };

        // Act
        BankAccountAggregate result = reducer.Apply(new(), @event);

        // Assert
        result.Balance.Should().Be(250.50m);
    }

    /// <summary>
    ///     Reducing AccountOpened should set HolderName from event.
    /// </summary>
    [Fact]
    public void AccountOpenedSetsHolderName()
    {
        // Arrange
        AccountOpened @event = new()
        {
            HolderName = "John Doe",
            InitialDeposit = 0m,
        };

        // Act
        BankAccountAggregate result = reducer.Apply(new(), @event);

        // Assert
        result.HolderName.Should().Be("John Doe");
    }

    /// <summary>
    ///     Reducing AccountOpened should set IsOpen to true.
    /// </summary>
    [Fact]
    public void AccountOpenedSetsIsOpenToTrue()
    {
        // Arrange
        AccountOpened @event = new()
        {
            HolderName = "Test",
            InitialDeposit = 100m,
        };

        // Act
        BankAccountAggregate result = reducer.Apply(new(), @event);

        // Assert
        result.IsOpen.Should().BeTrue();
    }

    /// <summary>
    ///     Reducing AccountOpened with null event should throw.
    /// </summary>
    [Fact]
    public void AccountOpenedWithNullEventThrows()
    {
        // Act
        Action act = () => reducer.Apply(new(), null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}