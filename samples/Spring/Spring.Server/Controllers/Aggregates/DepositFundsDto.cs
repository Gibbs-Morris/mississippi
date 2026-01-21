using System.Text.Json.Serialization;

using Mississippi.Common.Abstractions.Attributes;


namespace Spring.Server.Controllers.Aggregates;

/// <summary>
///     Request to deposit funds into a bank account.
/// </summary>
[PendingSourceGenerator]
public sealed record DepositFundsDto
{
    /// <summary>
    ///     Gets the amount to deposit.
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonRequired]
    public required decimal Amount { get; init; }
}