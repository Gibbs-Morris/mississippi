using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Aggregates.BankAccount.Handlers;


namespace Spring.Domain.L0Tests.Aggregates.BankAccount.Handlers;

/// <summary>
///     Tests for <see cref="WithdrawFundsHandler" />.
/// </summary>
public sealed class WithdrawFundsHandlerTests
{
    private readonly WithdrawFundsHandler handler = new();

    private static BankAccountAggregate AccountWithBalance(
        decimal balance
    ) =>
        new()
        {
            IsOpen = true,
            HolderName = "Test User",
            Balance = balance,
            DepositCount = 1,
            WithdrawalCount = 0,
        };

    /// <summary>
    ///     Withdrawing entire balance should succeed.
    /// </summary>
    [Fact]
    public void WithdrawEntireBalanceSucceeds()
    {
        // Arrange
        WithdrawFunds command = new()
        {
            Amount = 100m,
        };

        // Act
        IReadOnlyList<object> events = handler.ShouldSucceed(AccountWithBalance(100m), command);

        // Assert
        events.Should().ContainSingle();
        FundsWithdrawn withdrawn = events[0].Should().BeOfType<FundsWithdrawn>().Subject;
        withdrawn.Amount.Should().Be(100m);
    }

    /// <summary>
    ///     Withdrawing from closed account should fail with InvalidState.
    /// </summary>
    [Fact]
    public void WithdrawFromClosedAccountFailsWithInvalidState()
    {
        // Arrange
        BankAccountAggregate closedAccount = new()
        {
            IsOpen = false,
            Balance = 100m,
        };
        WithdrawFunds command = new()
        {
            Amount = 50m,
        };

        // Act & Assert
        handler.ShouldFailWithMessage(closedAccount, command, AggregateErrorCodes.InvalidState, "must be open");
    }

    /// <summary>
    ///     Withdrawing from null state should fail with InvalidState.
    /// </summary>
    [Fact]
    public void WithdrawFromNullStateFailsWithInvalidState()
    {
        // Arrange
        WithdrawFunds command = new()
        {
            Amount = 50m,
        };

        // Act & Assert
        handler.ShouldFail(null, command, AggregateErrorCodes.InvalidState);
    }

    /// <summary>
    ///     Withdrawing funds within balance should emit FundsWithdrawn event.
    /// </summary>
    [Fact]
    public void WithdrawFundsWithinBalanceEmitsFundsWithdrawn()
    {
        // Arrange
        WithdrawFunds command = new()
        {
            Amount = 50m,
        };

        // Act & Assert
        handler.ShouldEmit(
            AccountWithBalance(100m),
            command,
            new FundsWithdrawn
            {
                Amount = 50m,
            });
    }

    /// <summary>
    ///     Withdrawing more than balance should fail with InvalidCommand.
    /// </summary>
    [Fact]
    public void WithdrawMoreThanBalanceFailsWithInvalidCommand()
    {
        // Arrange
        WithdrawFunds command = new()
        {
            Amount = 150m,
        };

        // Act & Assert
        handler.ShouldFailWithMessage(
            AccountWithBalance(100m),
            command,
            AggregateErrorCodes.InvalidCommand,
            "Insufficient funds");
    }

    /// <summary>
    ///     Withdrawing negative amount should fail with InvalidCommand.
    /// </summary>
    [Fact]
    public void WithdrawNegativeAmountFailsWithInvalidCommand()
    {
        // Arrange
        WithdrawFunds command = new()
        {
            Amount = -50m,
        };

        // Act & Assert
        handler.ShouldFail(AccountWithBalance(100m), command, AggregateErrorCodes.InvalidCommand);
    }

    /// <summary>
    ///     Withdrawing zero amount should fail with InvalidCommand.
    /// </summary>
    [Fact]
    public void WithdrawZeroAmountFailsWithInvalidCommand()
    {
        // Arrange
        WithdrawFunds command = new()
        {
            Amount = 0m,
        };

        // Act & Assert
        handler.ShouldFailWithMessage(
            AccountWithBalance(100m),
            command,
            AggregateErrorCodes.InvalidCommand,
            "must be positive");
    }
}