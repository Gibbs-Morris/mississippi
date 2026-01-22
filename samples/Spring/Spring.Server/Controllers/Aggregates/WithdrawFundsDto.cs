using System.Text.Json.Serialization;

using Mississippi.Sdk.Generators.Abstractions;


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
    [JsonPropertyName("amount")]
    [JsonRequired]
    public required decimal Amount { get; init; }
}