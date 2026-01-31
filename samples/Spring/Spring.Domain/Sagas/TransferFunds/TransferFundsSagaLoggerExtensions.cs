using System;

using Microsoft.Extensions.Logging;


namespace Spring.Domain.Sagas.TransferFunds;

/// <summary>
///     High-performance logging extensions for the TransferFunds saga.
/// </summary>
internal static partial class TransferFundsSagaLoggerExtensions
{
    /// <summary>
    ///     Logs when a debit operation succeeds.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="accountId">The source account identifier.</param>
    /// <param name="amount">The amount debited.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Saga {SagaId}: Debited £{Amount} from account {AccountId}")]
    public static partial void LogDebitSucceeded(
        this ILogger logger,
        Guid sagaId,
        string accountId,
        decimal amount
    );

    /// <summary>
    ///     Logs when a debit operation fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="accountId">The source account identifier.</param>
    /// <param name="errorMessage">The error message.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Saga {SagaId}: Failed to debit account {AccountId}: {ErrorMessage}")]
    public static partial void LogDebitFailed(
        this ILogger logger,
        Guid sagaId,
        string accountId,
        string? errorMessage
    );

    /// <summary>
    ///     Logs when a credit operation succeeds.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="accountId">The destination account identifier.</param>
    /// <param name="amount">The amount credited.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Saga {SagaId}: Credited £{Amount} to account {AccountId}")]
    public static partial void LogCreditSucceeded(
        this ILogger logger,
        Guid sagaId,
        string accountId,
        decimal amount
    );

    /// <summary>
    ///     Logs when a credit operation fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="accountId">The destination account identifier.</param>
    /// <param name="errorMessage">The error message.</param>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Saga {SagaId}: Failed to credit account {AccountId}: {ErrorMessage}")]
    public static partial void LogCreditFailed(
        this ILogger logger,
        Guid sagaId,
        string accountId,
        string? errorMessage
    );

    /// <summary>
    ///     Logs when a refund operation succeeds.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="accountId">The source account identifier.</param>
    /// <param name="amount">The amount refunded.</param>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Saga {SagaId}: Refunded £{Amount} to account {AccountId}")]
    public static partial void LogRefundSucceeded(
        this ILogger logger,
        Guid sagaId,
        string accountId,
        decimal amount
    );

    /// <summary>
    ///     Logs when a refund operation fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="accountId">The source account identifier.</param>
    /// <param name="errorMessage">The error message.</param>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Saga {SagaId}: Failed to refund account {AccountId}: {ErrorMessage}")]
    public static partial void LogRefundFailed(
        this ILogger logger,
        Guid sagaId,
        string accountId,
        string? errorMessage
    );

    /// <summary>
    ///     Logs when a compensation is skipped.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="reason">The reason for skipping.</param>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Saga {SagaId}: Skipping compensation - {Reason}")]
    public static partial void LogSkippingCompensation(
        this ILogger logger,
        Guid sagaId,
        string reason
    );
}
