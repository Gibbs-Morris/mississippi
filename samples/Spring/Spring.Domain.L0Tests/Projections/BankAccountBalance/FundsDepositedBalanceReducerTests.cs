using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Projections.BankAccountBalance;
using Spring.Domain.Projections.BankAccountBalance.Reducers;


namespace Spring.Domain.L0Tests.Projections.BankAccountBalance;

/// <summary>
///     Tests for <see cref="FundsDepositedBalanceReducer" />.
/// </summary>
public sealed class FundsDepositedBalanceReducerTests
{
    private readonly FundsDepositedBalanceReducer reducer = new();

    /// <summary>
    ///     Verifies that depositing funds increases the balance.
    /// </summary>
    [Fact]
    public void ReduceWithFundsDepositedIncreasesBalance()
    {
        // Arrange
        BankAccountBalanceProjection initial = new()
        {
            HolderName = "John Doe",
            Balance = 500.00m,
            IsOpen = true,
        };
        FundsDeposited evt = new()
        {
            Amount = 250.00m,
        };

        // Act
        BankAccountBalanceProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Balance.Should().Be(750.00m);
    }

    /// <summary>
    ///     Verifies that depositing funds preserves other projection properties.
    /// </summary>
    [Fact]
    public void ReduceWithFundsDepositedPreservesOtherProperties()
    {
        // Arrange
        BankAccountBalanceProjection initial = new()
        {
            HolderName = "Jane Doe",
            Balance = 100.00m,
            IsOpen = true,
        };
        FundsDeposited evt = new()
        {
            Amount = 50.00m,
        };

        // Act
        BankAccountBalanceProjection result = reducer.Apply(initial, evt);

        // Assert
        result.HolderName.Should().Be("Jane Doe");
        result.IsOpen.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that reducer handles large deposit amounts.
    /// </summary>
    [Fact]
    public void ReduceWithLargeDepositHandlesCorrectly()
    {
        // Arrange
        BankAccountBalanceProjection initial = new()
        {
            Balance = 1_000_000.00m,
            IsOpen = true,
        };
        FundsDeposited evt = new()
        {
            Amount = 999_999_999.99m,
        };

        // Act
        BankAccountBalanceProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Balance.Should().Be(1_000_999_999.99m);
    }

    /// <summary>
    ///     Verifies that multiple deposits accumulate correctly.
    /// </summary>
    [Fact]
    public void ReduceWithMultipleDepositsAccumulatesBalance()
    {
        // Arrange
        BankAccountBalanceProjection state = new()
        {
            HolderName = "Accumulator",
            Balance = 0m,
            IsOpen = true,
        };

        // Act
        state = reducer.Apply(
            state,
            new()
            {
                Amount = 100.00m,
            });
        state = reducer.Apply(
            state,
            new()
            {
                Amount = 200.00m,
            });
        state = reducer.Apply(
            state,
            new()
            {
                Amount = 50.00m,
            });

        // Assert
        state.Balance.Should().Be(350.00m);
    }

    /// <summary>
    ///     Verifies that reducer throws when event is null.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrowsArgumentNullException()
    {
        // Arrange
        BankAccountBalanceProjection initial = new()
        {
            HolderName = "Test",
            Balance = 100.00m,
            IsOpen = true,
        };

        // Act & Assert
        reducer.ShouldThrow<ArgumentNullException, FundsDeposited, BankAccountBalanceProjection>(
            initial,
            null!,
            "eventData");
    }

    /// <summary>
    ///     Verifies that depositing zero does not change balance.
    /// </summary>
    [Fact]
    public void ReduceWithZeroDepositKeepsBalanceUnchanged()
    {
        // Arrange
        BankAccountBalanceProjection initial = new()
        {
            HolderName = "Test",
            Balance = 100.00m,
            IsOpen = true,
        };
        FundsDeposited evt = new()
        {
            Amount = 0m,
        };

        // Act
        BankAccountBalanceProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Balance.Should().Be(100.00m);
    }
}