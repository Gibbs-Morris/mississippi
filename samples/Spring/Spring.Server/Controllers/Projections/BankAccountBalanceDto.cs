using System.Text.Json.Serialization;

using Mississippi.Common.Abstractions.Attributes;


namespace Spring.Server.Controllers.Projections;

/// <summary>
///     Response DTO for the bank account balance projection.
/// </summary>
[PendingSourceGenerator]
public sealed record BankAccountBalanceDto
{
    /// <summary>
    ///     Gets the current balance of the account.
    /// </summary>
    [JsonRequired]
    public required decimal Balance { get; init; }

    /// <summary>
    ///     Gets the account holder name.
    /// </summary>
    [JsonRequired]
    public required string HolderName { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the account is open.
    /// </summary>
    [JsonRequired]
    public required bool IsOpen { get; init; }
}