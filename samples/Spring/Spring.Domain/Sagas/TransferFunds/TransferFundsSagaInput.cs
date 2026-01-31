using Orleans;


namespace Spring.Domain.Sagas.TransferFunds;

/// <summary>
///     Input data required to start a TransferFunds saga.
/// </summary>
[GenerateSerializer]
[Alias("Spring.Domain.Sagas.TransferFunds.TransferFundsSagaInput")]
public sealed record TransferFundsSagaInput
{
    /// <summary>
    ///     Gets the amount to transfer.
    /// </summary>
    [Id(2)]
    public required decimal Amount { get; init; }

    /// <summary>
    ///     Gets the account to deposit funds into.
    /// </summary>
    [Id(1)]
    public required string DestinationAccountId { get; init; }

    /// <summary>
    ///     Gets the account to withdraw funds from.
    /// </summary>
    [Id(0)]
    public required string SourceAccountId { get; init; }
}