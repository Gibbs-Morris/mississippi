using System.Text.Json.Serialization;

using Mississippi.Common.Abstractions.Attributes;


namespace Spring.Server.Controllers.Aggregates;

/// <summary>
///     Request to withdraw funds.
/// </summary>
[PendingSourceGenerator]
public sealed record WithdrawDto
{
    /// <summary>
    ///     Gets the amount to withdraw.
    /// </summary>
    [JsonRequired]
    public required decimal Amount { get; init; }
}