using Allure.Xunit.Attributes;

using FluentAssertions;

using Mississippi.EventSourcing.Aggregates.Abstractions;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Commands;
using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Aggregates.BankAccount.Handlers;

using Xunit;


namespace Spring.Domain.L0Tests.Aggregates.BankAccount.Handlers;

/// <summary>
///     Tests for <see cref="OpenAccountHandler" />.
/// </summary>
[AllureParentSuite("Spring Domain")]
[AllureSuite("Aggregates")]
[AllureSubSuite("OpenAccountHandler")]
public sealed class OpenAccountHandlerTests
{
    private readonly OpenAccountHandler handler = new();

    /// <summary>
    ///     Opening an account with valid data should emit AccountOpened event.
    /// </summary>
    [Fact]
    [AllureFeature("Account Opening")]
    public void OpenAccountWithValidDataEmitsAccountOpened()
    {
        // Arrange
        OpenAccount command = new("John Doe", 100m);

        // Act & Assert
        handler.ShouldEmit(
            null,
            command,
            new AccountOpened { HolderName = "John Doe", InitialDeposit = 100m });
    }

    /// <summary>
    ///     Opening an account with zero initial deposit should succeed.
    /// </summary>
    [Fact]
    [AllureFeature("Account Opening")]
    public void OpenAccountWithZeroDepositSucceeds()
    {
        // Arrange
        OpenAccount command = new("Jane Doe");

        // Act
        var events = handler.ShouldSucceed(null, command);

        // Assert
        events.Should().ContainSingle();
        events[0].Should().BeEquivalentTo(
            new AccountOpened { HolderName = "Jane Doe", InitialDeposit = 0m });
    }

    /// <summary>
    ///     Opening an already open account should fail with AlreadyExists.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void OpenAlreadyOpenAccountFailsWithAlreadyExists()
    {
        // Arrange
        BankAccountAggregate existingAccount = new() { IsOpen = true, HolderName = "Existing" };
        OpenAccount command = new("New Owner", 50m);

        // Act & Assert
        handler.ShouldFail(existingAccount, command, AggregateErrorCodes.AlreadyExists);
    }

    /// <summary>
    ///     Opening an account with empty holder name should fail with InvalidCommand.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void OpenAccountWithEmptyNameFailsWithInvalidCommand()
    {
        // Arrange
        OpenAccount command = new(string.Empty, 100m);

        // Act & Assert
        handler.ShouldFailWithMessage(
            null,
            command,
            AggregateErrorCodes.InvalidCommand,
            "holder name is required");
    }

    /// <summary>
    ///     Opening an account with whitespace-only holder name should fail.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void OpenAccountWithWhitespaceNameFailsWithInvalidCommand()
    {
        // Arrange
        OpenAccount command = new("   ", 100m);

        // Act & Assert
        handler.ShouldFail(null, command, AggregateErrorCodes.InvalidCommand);
    }

    /// <summary>
    ///     Opening an account with negative initial deposit should fail.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void OpenAccountWithNegativeDepositFailsWithInvalidCommand()
    {
        // Arrange
        OpenAccount command = new("Test", -50m);

        // Act & Assert
        handler.ShouldFailWithMessage(
            null,
            command,
            AggregateErrorCodes.InvalidCommand,
            "cannot be negative");
    }
}
