using System.Text.Json.Serialization;

using Mississippi.Common.Abstractions.Attributes;


namespace Spring.Server.Controllers.Aggregates;

/// <summary>
///     Request to withdraw funds from a bank account.
/// </summary>
[PendingSourceGenerator]
public sealed record WithdrawFundsDto
{
    /// <summary>
    ///     Gets the amount to withdraw.
    /// </summary>
    [JsonRequired]
    public required decimal Amount { get; init; }
}