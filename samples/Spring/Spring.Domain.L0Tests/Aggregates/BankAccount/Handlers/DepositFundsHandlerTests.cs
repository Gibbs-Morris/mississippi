using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Aggregates.BankAccount.Handlers;


namespace Spring.Domain.L0Tests.Aggregates.BankAccount.Handlers;

/// <summary>
///     Tests for <see cref="DepositFundsHandler" />.
/// </summary>
public sealed class DepositFundsHandlerTests
{
    private readonly DepositFundsHandler handler = new();

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
    ///     Depositing funds to an open account should emit FundsDeposited event.
    /// </summary>
    [Fact]
    public void DepositFundsToOpenAccountEmitsFundsDeposited()
    {
        // Arrange
        DepositFunds command = new()
        {
            Amount = 50m,
        };

        // Act & Assert
        handler.ShouldEmit(
            OpenAccount,
            command,
            new FundsDeposited
            {
                Amount = 50m,
            });
    }

    /// <summary>
    ///     Depositing a large amount should succeed.
    /// </summary>
    [Fact]
    public void DepositLargeAmountSucceeds()
    {
        // Arrange
        DepositFunds command = new()
        {
            Amount = 1_000_000m,
        };

        // Act
        IReadOnlyList<object> events = handler.ShouldSucceed(OpenAccount, command);

        // Assert
        events.Should().ContainSingle();
        FundsDeposited deposited = events[0].Should().BeOfType<FundsDeposited>().Subject;
        deposited.Amount.Should().Be(1_000_000m);
    }

    /// <summary>
    ///     Depositing negative amount should fail with InvalidCommand.
    /// </summary>
    [Fact]
    public void DepositNegativeAmountFailsWithInvalidCommand()
    {
        // Arrange
        DepositFunds command = new()
        {
            Amount = -50m,
        };

        // Act & Assert
        handler.ShouldFail(OpenAccount, command, AggregateErrorCodes.InvalidCommand);
    }

    /// <summary>
    ///     Depositing to a closed account should fail with InvalidState.
    /// </summary>
    [Fact]
    public void DepositToClosedAccountFailsWithInvalidState()
    {
        // Arrange
        BankAccountAggregate closedAccount = new()
        {
            IsOpen = false,
        };
        DepositFunds command = new()
        {
            Amount = 50m,
        };

        // Act & Assert
        handler.ShouldFailWithMessage(closedAccount, command, AggregateErrorCodes.InvalidState, "must be open");
    }

    /// <summary>
    ///     Depositing to null state (no account) should fail with InvalidState.
    /// </summary>
    [Fact]
    public void DepositToNullStateFailsWithInvalidState()
    {
        // Arrange
        DepositFunds command = new()
        {
            Amount = 50m,
        };

        // Act & Assert
        handler.ShouldFail(null, command, AggregateErrorCodes.InvalidState);
    }

    /// <summary>
    ///     Depositing zero amount should fail with InvalidCommand.
    /// </summary>
    [Fact]
    public void DepositZeroAmountFailsWithInvalidCommand()
    {
        // Arrange
        DepositFunds command = new()
        {
            Amount = 0m,
        };

        // Act & Assert
        handler.ShouldFailWithMessage(OpenAccount, command, AggregateErrorCodes.InvalidCommand, "must be positive");
    }
}