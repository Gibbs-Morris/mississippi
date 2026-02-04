using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Aggregates.MoneyTransferSaga.Commands;

/// <summary>
///     Command input to start a money transfer saga.
/// </summary>
[GenerateCommand(Route = "transfer")]
[GenerateSerializer]
[Alias("Spring.Domain.Aggregates.MoneyTransferSaga.Commands.StartMoneyTransferCommand")]
public sealed record StartMoneyTransferCommand
{
    /// <summary>
    ///     Gets the transfer amount.
    /// </summary>
    [Id(2)]
    public decimal Amount { get; init; }

    /// <summary>
    ///     Gets the destination account identifier.
    /// </summary>
    [Id(1)]
    public string DestinationAccountId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the source account identifier.
    /// </summary>
    [Id(0)]
    public string SourceAccountId { get; init; } = string.Empty;
}