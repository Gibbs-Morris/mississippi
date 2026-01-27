using Allure.Xunit.Attributes;

using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Projections.BankAccountBalance;
using Spring.Domain.Projections.BankAccountBalance.Reducers;


namespace Spring.Domain.L0Tests.Projections.BankAccountBalance;

/// <summary>
///     Tests for <see cref="AccountOpenedBalanceReducer" />.
/// </summary>
[AllureParentSuite("Spring Domain")]
[AllureSuite("Projections")]
[AllureSubSuite("BankAccountBalance - AccountOpened")]
public sealed class AccountOpenedBalanceReducerTests
{
    private readonly AccountOpenedBalanceReducer reducer = new();

    /// <summary>
    ///     Verifies that applying AccountOpened to an empty projection initializes all fields.
    /// </summary>
    [Fact]
    public void ReduceWithAccountOpenedInitializesProjection()
    {
        // Arrange
        BankAccountBalanceProjection initial = new();
        AccountOpened evt = new()
        {
            HolderName = "John Doe",
            InitialDeposit = 500.00m,
        };

        // Act
        BankAccountBalanceProjection result = reducer.Apply(initial, evt);

        // Assert
        result.HolderName.Should().Be("John Doe");
        result.Balance.Should().Be(500.00m);
        result.IsOpen.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that reducer produces an equivalent projection using fluent syntax.
    /// </summary>
    [Fact]
    public void ReduceWithAccountOpenedProducesExpectedProjection()
    {
        // Arrange
        BankAccountBalanceProjection initial = new();
        AccountOpened evt = new()
        {
            HolderName = "Test User",
            InitialDeposit = 1000.00m,
        };
        BankAccountBalanceProjection expected = new()
        {
            HolderName = "Test User",
            Balance = 1000.00m,
            IsOpen = true,
        };

        // Act & Assert
        reducer.ShouldProduce(initial, evt, expected);
    }

    /// <summary>
    ///     Verifies that reducer sets IsOpen to true.
    /// </summary>
    [Fact]
    public void ReduceWithAccountOpenedSetsIsOpenToTrue()
    {
        // Arrange
        BankAccountBalanceProjection initial = new()
        {
            IsOpen = false,
        };
        AccountOpened evt = new()
        {
            HolderName = "Jane Doe",
            InitialDeposit = 0m,
        };

        // Act
        BankAccountBalanceProjection result = reducer.Apply(initial, evt);

        // Assert
        result.IsOpen.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that reducer throws when event is null.
    /// </summary>
    [Fact]
    public void ReduceWithNullEventThrowsArgumentNullException()
    {
        // Arrange
        BankAccountBalanceProjection initial = new();

        // Act & Assert
        reducer.ShouldThrow<ArgumentNullException, AccountOpened, BankAccountBalanceProjection>(
            initial,
            null!,
            "eventData");
    }

    /// <summary>
    ///     Verifies that reducer handles zero initial deposit.
    /// </summary>
    [Fact]
    public void ReduceWithZeroInitialDepositSetsBalanceToZero()
    {
        // Arrange
        BankAccountBalanceProjection initial = new();
        AccountOpened evt = new()
        {
            HolderName = "Zero Balance",
            InitialDeposit = 0m,
        };

        // Act
        BankAccountBalanceProjection result = reducer.Apply(initial, evt);

        // Assert
        result.Balance.Should().Be(0m);
        result.HolderName.Should().Be("Zero Balance");
    }
}