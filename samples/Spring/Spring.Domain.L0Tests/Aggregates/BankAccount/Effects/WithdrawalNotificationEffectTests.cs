using System.Threading;
using System.Threading.Tasks;

using Moq;

using Spring.Domain.Aggregates.BankAccount;
using Spring.Domain.Aggregates.BankAccount.Effects;
using Spring.Domain.Aggregates.BankAccount.Events;
using Spring.Domain.Services;


namespace Spring.Domain.L0Tests.Aggregates.BankAccount.Effects;

/// <summary>
///     Unit tests for <see cref="WithdrawalNotificationEffect" />.
/// </summary>
/// <remarks>
///     <para>
///         These tests verify the fire-and-forget notification behavior for withdrawals.
///         The effect sends notifications through an <see cref="INotificationService" />
///         without blocking the command flow.
///     </para>
///     <para>
///         This test class demonstrates the testing pattern for fire-and-forget effects
///         using <see cref="FireAndForgetEffectTestHarness{TEffect,TEvent,TAggregate}" />.
///     </para>
/// </remarks>
public sealed class WithdrawalNotificationEffectTests
{
    private const string TestAccountId = "acc-123";

    private const string TestBrookKey = "SPRING.BANKING.ACCOUNT|acc-123";

    /// <summary>
    ///     Verifies that OperationCanceledException is not swallowed.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    public async Task CancellationExceptionShouldNotBeSwallowedAsync()
    {
        // Arrange
        Mock<INotificationService> notificationServiceMock = new();
        notificationServiceMock.Setup(s => s.SendWithdrawalAlertAsync(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate> harness =
            FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate>.Create()
                .WithBrookKey(TestBrookKey);
        WithdrawalNotificationEffect effect = harness.Build(logger => new(notificationServiceMock.Object, logger));
        FundsWithdrawn eventData = new()
        {
            Amount = 100m,
        };
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = 900m,
            IsOpen = true,
        };

        // Act
        Func<Task> act = () => harness.InvokeAsync(effect, eventData, state);

        // Assert - OperationCanceledException should propagate
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    ///     Verifies that notification failures are handled gracefully without throwing.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    public async Task NotificationFailureShouldNotThrowAsync()
    {
        // Arrange
        Mock<INotificationService> notificationServiceMock = new();
        notificationServiceMock.Setup(s => s.SendWithdrawalAlertAsync(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Network error"));
        FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate> harness =
            FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate>.Create()
                .WithBrookKey(TestBrookKey);
        WithdrawalNotificationEffect effect = harness.Build(logger => new(notificationServiceMock.Object, logger));
        FundsWithdrawn eventData = new()
        {
            Amount = 250m,
        };
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = 750m,
            IsOpen = true,
        };

        // Act - should not throw
        Func<Task> act = () => harness.InvokeAsync(effect, eventData, state);

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    ///     Verifies that null aggregate state throws ArgumentNullException.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    public async Task NullAggregateStateShouldThrowAsync()
    {
        // Arrange
        Mock<INotificationService> notificationServiceMock = new();
        FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate> harness =
            FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate>.Create()
                .WithBrookKey(TestBrookKey);
        WithdrawalNotificationEffect effect = harness.Build(logger => new(notificationServiceMock.Object, logger));
        FundsWithdrawn eventData = new()
        {
            Amount = 100m,
        };

        // Act
        Func<Task> act = () => harness.InvokeAsync(effect, eventData, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("aggregateState");
    }

    /// <summary>
    ///     Verifies that null event data throws ArgumentNullException.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    public async Task NullEventDataShouldThrowAsync()
    {
        // Arrange
        Mock<INotificationService> notificationServiceMock = new();
        FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate> harness =
            FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate>.Create()
                .WithBrookKey(TestBrookKey);
        WithdrawalNotificationEffect effect = harness.Build(logger => new(notificationServiceMock.Object, logger));
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = 1000m,
            IsOpen = true,
        };

        // Act
        Func<Task> act = () => harness.InvokeAsync(effect, null!, state);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("eventData");
    }

    /// <summary>
    ///     Verifies that the effect extracts the account ID from the brook key correctly.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    public async Task ShouldExtractAccountIdFromBrookKeyAsync()
    {
        // Arrange
        const string customBrookKey = "SPRING.BANKING.ACCOUNT|custom-account-456";
        string? capturedAccountId = null;
        Mock<INotificationService> notificationServiceMock = new();
        notificationServiceMock.Setup(s => s.SendWithdrawalAlertAsync(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, decimal, decimal, CancellationToken>((
                accountId,
                _,
                _,
                _
            ) => capturedAccountId = accountId)
            .Returns(Task.CompletedTask);
        FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate> harness =
            FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate>.Create()
                .WithBrookKey(customBrookKey);
        WithdrawalNotificationEffect effect = harness.Build(logger => new(notificationServiceMock.Object, logger));
        FundsWithdrawn eventData = new()
        {
            Amount = 100m,
        };
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = 900m,
            IsOpen = true,
        };

        // Act
        await harness.InvokeAsync(effect, eventData, state);

        // Assert
        capturedAccountId.Should().Be("custom-account-456");
    }

    /// <summary>
    ///     Verifies that the effect passes remaining balance correctly.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    public async Task ShouldPassRemainingBalanceFromAggregateStateAsync()
    {
        // Arrange
        decimal? capturedRemainingBalance = null;
        Mock<INotificationService> notificationServiceMock = new();
        notificationServiceMock.Setup(s => s.SendWithdrawalAlertAsync(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, decimal, decimal, CancellationToken>((
                _,
                _,
                balance,
                _
            ) => capturedRemainingBalance = balance)
            .Returns(Task.CompletedTask);
        FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate> harness =
            FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate>.Create()
                .WithBrookKey(TestBrookKey);
        WithdrawalNotificationEffect effect = harness.Build(logger => new(notificationServiceMock.Object, logger));
        FundsWithdrawn eventData = new()
        {
            Amount = 1000m,
        };
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = 4567.89m, // The remaining balance after withdrawal
            IsOpen = true,
        };

        // Act
        await harness.InvokeAsync(effect, eventData, state);

        // Assert
        capturedRemainingBalance.Should().Be(4567.89m);
    }

    /// <summary>
    ///     Verifies that cancellation is properly propagated to the notification service.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    public async Task ShouldPropagateCancellationTokenAsync()
    {
        // Arrange
        CancellationToken capturedToken = default;
        Mock<INotificationService> notificationServiceMock = new();
        notificationServiceMock.Setup(s => s.SendWithdrawalAlertAsync(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, decimal, decimal, CancellationToken>((
                _,
                _,
                _,
                token
            ) => capturedToken = token)
            .Returns(Task.CompletedTask);
        FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate> harness =
            FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate>.Create()
                .WithBrookKey(TestBrookKey);
        WithdrawalNotificationEffect effect = harness.Build(logger => new(notificationServiceMock.Object, logger));
        FundsWithdrawn eventData = new()
        {
            Amount = 100m,
        };
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = 900m,
            IsOpen = true,
        };
        using CancellationTokenSource cts = new();
        CancellationToken expectedToken = cts.Token;

        // Act
        await harness.InvokeAsync(effect, eventData, state, expectedToken);

        // Assert
        capturedToken.Should().Be(expectedToken);
    }

    /// <summary>
    ///     Verifies that a withdrawal triggers a notification with correct parameters.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    public async Task WithdrawalShouldSendNotificationWithCorrectParametersAsync()
    {
        // Arrange
        Mock<INotificationService> notificationServiceMock = new();
        notificationServiceMock.Setup(s => s.SendWithdrawalAlertAsync(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate> harness =
            FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate>.Create()
                .WithBrookKey(TestBrookKey)
                .WithEventPosition(42);
        WithdrawalNotificationEffect effect = harness.Build(logger => new(notificationServiceMock.Object, logger));
        FundsWithdrawn eventData = new()
        {
            Amount = 500m,
        };
        BankAccountAggregate state = new()
        {
            HolderName = "Test User",
            Balance = 1500m, // Balance after withdrawal
            IsOpen = true,
        };

        // Act
        await harness.InvokeAsync(effect, eventData, state);

        // Assert
        notificationServiceMock.Verify(
            s => s.SendWithdrawalAlertAsync(TestAccountId, 500m, 1500m, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies that zero-amount withdrawals still send notifications.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test.</returns>
    [Fact]
    public async Task ZeroAmountWithdrawalShouldStillSendNotificationAsync()
    {
        // Arrange
        Mock<INotificationService> notificationServiceMock = new();
        notificationServiceMock.Setup(s => s.SendWithdrawalAlertAsync(
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate> harness =
            FireAndForgetEffectTestHarness<WithdrawalNotificationEffect, FundsWithdrawn, BankAccountAggregate>.Create()
                .WithBrookKey(TestBrookKey);
        WithdrawalNotificationEffect effect = harness.Build(logger => new(notificationServiceMock.Object, logger));
        FundsWithdrawn eventData = new()
        {
            Amount = 0m,
        };
        BankAccountAggregate state = new()
        {
            HolderName = "Test",
            Balance = 1000m,
            IsOpen = true,
        };

        // Act
        await harness.InvokeAsync(effect, eventData, state);

        // Assert - notification should still be sent (business may want audit trail)
        notificationServiceMock.Verify(
            s => s.SendWithdrawalAlertAsync(TestAccountId, 0m, 1000m, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}