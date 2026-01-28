using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Projections.BankAccountBalance;
using Spring.Domain.Projections.BankAccountBalance.Reducers;


namespace Spring.Domain.L0Tests.Projections.BankAccountBalance;

/// <summary>
///     Tests for <see cref="FundsWithdrawnBalanceReducer" />.
/// </summary>
public sealed class FundsWithdrawnBalanceReducerTests
{
    private readonly FundsWithdrawnBalanceReducer reducer = new();

    /// <summary>
    ///     Verifies that withdrawing funds decreases the balance.
    /// </summary>
    [Fact]
    public void ReduceWithFundsWithdrawnDecreasesBalance()
    {
        // Arrange
        BankAccountBalanceProjection initial = new()
        {
            HolderName = "John Doe",
            Balance = 500.00m,
            IsOpen = true,
        };
        FundsWithdrawn evt = new()
        {
            Amount = 150.00m,
        };

        // Act
        BankAccountBalanceProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Balance.Should().Be(350.00m);
    }

    /// <summary>
    ///     Verifies that withdrawing funds preserves other projection properties.
    /// </summary>
    [Fact]
    public void ReduceWithFundsWithdrawnPreservesOtherProperties()
    {
        // Arrange
        BankAccountBalanceProjection initial = new()
        {
            HolderName = "Jane Doe",
            Balance = 200.00m,
            IsOpen = true,
        };
        FundsWithdrawn evt = new()
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
    ///     Verifies that multiple withdrawals accumulate correctly.
    /// </summary>
    [Fact]
    public void ReduceWithMultipleWithdrawalsDecreasesBalanceCorrectly()
    {
        // Arrange
        BankAccountBalanceProjection state = new()
        {
            HolderName = "Spender",
            Balance = 1000.00m,
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
        state.Balance.Should().Be(650.00m);
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
        reducer.ShouldThrow<ArgumentNullException, FundsWithdrawn, BankAccountBalanceProjection>(
            initial,
            null!,
            "eventData");
    }

    /// <summary>
    ///     Verifies that reducer can result in negative balance (overdraft).
    ///     Note: Business rules for overdraft prevention should be in the aggregate, not the reducer.
    /// </summary>
    [Fact]
    public void ReduceWithWithdrawalExceedingBalanceResultsInNegativeBalance()
    {
        // Arrange
        BankAccountBalanceProjection initial = new()
        {
            HolderName = "Overdraft",
            Balance = 50.00m,
            IsOpen = true,
        };
        FundsWithdrawn evt = new()
        {
            Amount = 100.00m,
        };

        // Act
        BankAccountBalanceProjection result = reducer.Apply(initial, evt);

        // Assert - Reducer simply applies the math; aggregate enforces business rules
        result.Balance.Should().Be(-50.00m);
    }

    /// <summary>
    ///     Verifies that withdrawing zero does not change balance.
    /// </summary>
    [Fact]
    public void ReduceWithZeroWithdrawalKeepsBalanceUnchanged()
    {
        // Arrange
        BankAccountBalanceProjection initial = new()
        {
            HolderName = "Test",
            Balance = 100.00m,
            IsOpen = true,
        };
        FundsWithdrawn evt = new()
        {
            Amount = 0m,
        };

        // Act
        BankAccountBalanceProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Balance.Should().Be(100.00m);
    }
}