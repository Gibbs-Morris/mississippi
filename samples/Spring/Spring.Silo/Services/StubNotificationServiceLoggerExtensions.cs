using Microsoft.Extensions.Logging;


namespace Spring.Silo.Services;

/// <summary>
///     High-performance logging extensions for the stub notification service.
/// </summary>
internal static partial class StubNotificationServiceLoggerExtensions
{
    /// <summary>
    ///     Logs a withdrawal notification (stub implementation).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="accountId">The account ID.</param>
    /// <param name="amount">The withdrawal amount.</param>
    /// <param name="remainingBalance">The remaining balance.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "[STUB] Would send withdrawal notification: account {AccountId}, £{Amount} withdrawn, £{RemainingBalance} remaining")]
    public static partial void LogWithdrawalNotification(
        this ILogger logger,
        string accountId,
        decimal amount,
        decimal remainingBalance
    );
}
