using System;

using Mississippi.Inlet.Generators.Abstractions;

using Orleans;

namespace Spring.Domain.Aggregates.BankAccount.Commands;

/// <summary>
///     Command to debit funds from a bank account as part of an outgoing transfer.
/// </summary>
/// <remarks>
///     This command is typically invoked by the transfer saga during the debit step.
///     It is distinct from <see cref="WithdrawCash" /> which represents external cash withdrawal.
/// </remarks>
[GenerateCommand(Route = "debit-for-transfer")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Commands.DebitForTransfer")]
public sealed record DebitForTransfer
{
    /// <summary>
    ///     Gets the amount to debit.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; init; }

    /// <summary>
    ///     Gets the correlation ID linking this debit to the transfer saga.
    /// </summary>
    [Id(1)]
    public required Guid TransferCorrelationId { get; init; }

    /// <summary>
    ///     Gets the destination account ID for the transfer.
    /// </summary>
    [Id(2)]
    public required Guid DestinationAccountId { get; init; }
}
