using System;

using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Commands;

/// <summary>
///     Command to credit funds to a bank account as part of an incoming transfer.
/// </summary>
/// <remarks>
///     This command is typically invoked by the transfer saga during the credit step.
///     It is distinct from <see cref="DepositCash" /> which represents external cash deposit.
/// </remarks>
[GenerateCommand(Route = "credit-from-transfer")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Commands.CreditFromTransfer")]
public sealed record CreditFromTransfer
{
    /// <summary>
    ///     Gets the amount to credit.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; init; }

    /// <summary>
    ///     Gets the source account ID for the transfer.
    /// </summary>
    [Id(2)]
    public required Guid SourceAccountId { get; init; }

    /// <summary>
    ///     Gets the correlation ID linking this credit to the transfer saga.
    /// </summary>
    [Id(1)]
    public required Guid TransferCorrelationId { get; init; }
}