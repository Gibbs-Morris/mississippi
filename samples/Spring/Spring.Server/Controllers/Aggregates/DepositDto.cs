using System.Text.Json.Serialization;

using Mississippi.Common.Abstractions.Attributes;


namespace Spring.Server.Controllers.Aggregates;

/// <summary>
///     Request to deposit funds.
/// </summary>
[PendingSourceGenerator]
public sealed record DepositDto
{
    /// <summary>
    ///     Gets the amount to deposit.
    /// </summary>
    [JsonRequired]
    public required decimal Amount { get; init; }
}