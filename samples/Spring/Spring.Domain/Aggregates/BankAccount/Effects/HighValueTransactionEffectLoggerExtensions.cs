using Microsoft.Extensions.Logging;


namespace Spring.Domain.Aggregates.BankAccount.Effects;

/// <summary>
///     High-performance logging extensions for the high-value transaction effect.
/// </summary>
internal static partial class HighValueTransactionEffectLoggerExtensions
{
    /// <summary>
    ///     Logs when flagging a transaction failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="accountId">The account ID that failed to flag.</param>
    /// <param name="amount">The amount that failed to flag.</param>
    /// <param name="errorCode">The error code from the operation.</param>
    /// <param name="errorMessage">The error message from the operation.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to flag transaction on account {AccountId} for £{Amount}: {ErrorCode} - {ErrorMessage}")]
    public static partial void LogFlagTransactionFailed(
        this ILogger logger,
        string accountId,
        decimal amount,
        string errorCode,
        string errorMessage
    );

    /// <summary>
    ///     Logs when a high-value transaction is detected.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="accountId">The account ID where the deposit occurred.</param>
    /// <param name="amount">The deposit amount.</param>
    /// <param name="threshold">The AML threshold amount.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "High-value transaction detected on account {AccountId}: £{Amount} exceeds AML threshold of £{Threshold}")]
    public static partial void LogHighValueTransactionDetected(
        this ILogger logger,
        string accountId,
        decimal amount,
        decimal threshold
    );

    /// <summary>
    ///     Logs when a transaction has been flagged for investigation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="accountId">The account ID that was flagged.</param>
    /// <param name="amount">The flagged amount.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Transaction flagged for investigation: account {AccountId}, amount £{Amount}")]
    public static partial void LogTransactionFlagged(
        this ILogger logger,
        string accountId,
        decimal amount
    );
}