using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Commands;

/// <summary>
///     Command to deposit dollars (USD) into a bank account.
/// </summary>
/// <remarks>
///     This command is handled by <see cref="Handlers.DepositDollarsHandler" />
///     which yields a <see cref="Events.DollarsDeposited" /> event. That event
///     then triggers the <c>CurrencyConversionEffect</c> to fetch the exchange
///     rate and convert the deposit to GBP.
/// </remarks>
[GenerateCommand(Route = "deposit-dollars")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Commands.DepositDollars")]
public sealed record DepositDollars
{
    /// <summary>
    ///     Gets the amount to deposit in USD.
    /// </summary>
    [Id(0)]
    public decimal AmountUsd { get; init; }
}