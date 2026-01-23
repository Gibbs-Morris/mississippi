// NOTE: This file has been replaced by source generation.
// The generated version is in: obj/{Configuration}/net10.0/generated/Mississippi.Inlet.Server.Generators/
//     Mississippi.Inlet.Server.Generators.CommandServerDtoGenerator/WithdrawFundsDto.g.cs
// Keeping this file commented out for reference during the generator development phase.

#if false // Replaced by generated code
using System.Text.Json.Serialization;

using Mississippi.Inlet.Generators.Abstractions;


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
#endif