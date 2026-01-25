using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Events;

/// <summary>
///     Event raised when dollars (USD) are deposited into a bank account.
/// </summary>
/// <remarks>
///     This event triggers the <c>CurrencyConversionEffect</c> which fetches
///     the current USD/GBP exchange rate and yields a <see cref="ConvertedDollarsDeposited" />
///     event to record the converted amount in pounds.
/// </remarks>
[EventStorageName("SPRING", "BANKING", "DOLLARSDEPOSITED")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Events.DollarsDeposited")]
internal sealed record DollarsDeposited
{
    /// <summary>
    ///     Gets the amount deposited in USD.
    /// </summary>
    [Id(0)]
    public decimal AmountUsd { get; init; }
}