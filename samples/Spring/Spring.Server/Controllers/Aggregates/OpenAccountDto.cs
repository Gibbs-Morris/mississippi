// NOTE: This file has been replaced by source generation.
// The generated version is in: obj/{Configuration}/net10.0/generated/Mississippi.Sdk.Server.Generators/
//     Mississippi.Sdk.Server.Generators.CommandServerDtoGenerator/OpenAccountDto.g.cs
// Keeping this file commented out for reference during the generator development phase.

#if false // Replaced by generated code
using System.Text.Json.Serialization;

using Mississippi.Sdk.Generators.Abstractions;


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
    [JsonPropertyName("holderName")]
    [JsonRequired]
    public required string HolderName { get; init; }

    /// <summary>
    ///     Gets the initial deposit amount. Defaults to 0.
    /// </summary>
    [JsonPropertyName("initialDeposit")]
    public decimal InitialDeposit { get; init; } = 0m;
}
#endif