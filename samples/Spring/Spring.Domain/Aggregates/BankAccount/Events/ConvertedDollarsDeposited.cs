using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Events;

/// <summary>
///     Event raised when a USD deposit has been converted to GBP and credited.
/// </summary>
/// <remarks>
///     This event is yielded by the <c>CurrencyConversionEffect</c> after
///     successfully fetching the exchange rate from the Frankfurter API.
///     It records both the original USD amount and the converted GBP amount
///     for audit purposes.
/// </remarks>
[EventStorageName("SPRING", "BANKING", "CONVERTEDDOLLARSDEPOSITED")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Events.ConvertedDollarsDeposited")]
internal sealed record ConvertedDollarsDeposited
{
    /// <summary>
    ///     Gets the converted amount in GBP.
    /// </summary>
    [Id(1)]
    public decimal AmountGbp { get; init; }

    /// <summary>
    ///     Gets the original amount in USD.
    /// </summary>
    [Id(0)]
    public decimal AmountUsd { get; init; }

    /// <summary>
    ///     Gets the exchange rate used for conversion (USD to GBP).
    /// </summary>
    [Id(2)]
    public decimal ExchangeRate { get; init; }

    /// <summary>
    ///     Gets the date of the exchange rate.
    /// </summary>
    [Id(3)]
    public string RateDate { get; init; } = string.Empty;
}