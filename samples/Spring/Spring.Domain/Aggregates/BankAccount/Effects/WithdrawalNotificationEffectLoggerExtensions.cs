using Microsoft.Extensions.Logging;


namespace Spring.Domain.Aggregates.BankAccount.Effects;

/// <summary>
///     High-performance logging extensions for the withdrawal notification effect.
/// </summary>
internal static partial class WithdrawalNotificationEffectLoggerExtensions
{
    /// <summary>
    ///     Logs when starting to send a withdrawal notification.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="accountId">The account ID.</param>
    /// <param name="amount">The withdrawal amount.</param>
    /// <param name="remainingBalance">The remaining balance after withdrawal.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Sending withdrawal notification for account {AccountId}: £{Amount} withdrawn, £{RemainingBalance} remaining")]
    public static partial void LogSendingWithdrawalNotification(
        this ILogger logger,
        string accountId,
        decimal amount,
        decimal remainingBalance
    );

    /// <summary>
    ///     Logs when a withdrawal notification fails to send.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="accountId">The account ID.</param>
    /// <param name="amount">The withdrawal amount.</param>
    /// <param name="errorMessage">The error message.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to send withdrawal notification for account {AccountId}, £{Amount}: {ErrorMessage}")]
    public static partial void LogWithdrawalNotificationFailed(
        this ILogger logger,
        string accountId,
        decimal amount,
        string errorMessage
    );

    /// <summary>
    ///     Logs when a withdrawal notification was sent successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="accountId">The account ID.</param>
    /// <param name="amount">The withdrawal amount.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Withdrawal notification sent for account {AccountId}: £{Amount}")]
    public static partial void LogWithdrawalNotificationSent(
        this ILogger logger,
        string accountId,
        decimal amount
    );
}
