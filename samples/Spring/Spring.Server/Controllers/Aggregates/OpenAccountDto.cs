using System.Text.Json.Serialization;

using Mississippi.Common.Abstractions.Attributes;


namespace Spring.Server.Controllers.Aggregates;

/// <summary>
///     Request to open a bank account.
/// </summary>
[PendingSourceGenerator]
public sealed record OpenAccountDto
{
    /// <summary>
    ///     Gets the name of the account holder.
    /// </summary>
    [JsonRequired]
    public required string HolderName { get; init; }

    /// <summary>
    ///     Gets the initial deposit amount. Defaults to 0.
    /// </summary>
    public decimal InitialDeposit { get; init; } = 0m;
}