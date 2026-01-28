using System;

using Mississippi.Inlet.Generators.Abstractions;

using Orleans;


namespace Spring.Domain.Aggregates.BankAccount.Commands;

/// <summary>
///     Command to refund a failed transfer by restoring funds to the source account.
/// </summary>
/// <remarks>
///     This command is invoked by the transfer saga during compensation when a transfer
///     fails after the source account was debited.
/// </remarks>
[GenerateCommand(Route = "refund-transfer")]
[GenerateSerializer]
[Alias("Spring.Domain.BankAccount.Commands.RefundTransfer")]
public sealed record RefundTransfer
{
    /// <summary>
    ///     Gets the amount to refund.
    /// </summary>
    [Id(0)]
    public decimal Amount { get; init; }

    /// <summary>
    ///     Gets the reason for the refund.
    /// </summary>
    [Id(2)]
    public string? Reason { get; init; }

    /// <summary>
    ///     Gets the correlation ID linking this refund to the original transfer saga.
    /// </summary>
    [Id(1)]
    public required Guid TransferCorrelationId { get; init; }
}