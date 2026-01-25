using Microsoft.Extensions.Logging;


namespace Spring.Domain.Aggregates.BankAccount.Effects;

/// <summary>
///     High-performance logging extensions for currency conversion effect.
/// </summary>
internal static partial class CurrencyConversionLoggerExtensions
{
    /// <summary>
    ///     Logs when dollars are deposited and conversion is starting.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="amountUsd">The USD amount deposited.</param>
    [LoggerMessage(1, LogLevel.Information, "Dollars deposited: ${AmountUsd} USD. Fetching exchange rate...")]
    public static partial void LogDollarsDeposited(
        this ILogger logger,
        decimal amountUsd
    );

    /// <summary>
    ///     Logs when exchange rate is successfully applied.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="amountUsd">The original USD amount.</param>
    /// <param name="exchangeRate">The USD to GBP exchange rate.</param>
    /// <param name="amountGbp">The converted GBP amount.</param>
    /// <param name="rateDate">The date of the exchange rate.</param>
    [LoggerMessage(
        2,
        LogLevel.Information,
        "Exchange rate applied: ${AmountUsd} USD × {ExchangeRate} = £{AmountGbp} GBP (rate date: {RateDate})")]
    public static partial void LogExchangeRateApplied(
        this ILogger logger,
        decimal amountUsd,
        decimal exchangeRate,
        decimal amountGbp,
        string rateDate
    );

    /// <summary>
    ///     Logs when exchange rate fetch fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        3,
        LogLevel.Warning,
        "Failed to fetch exchange rate from Frankfurter API. USD deposit will not be converted.")]
    public static partial void LogExchangeRateFetchFailed(
        this ILogger logger
    );
}