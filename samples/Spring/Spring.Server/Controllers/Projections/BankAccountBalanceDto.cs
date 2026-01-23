// =============================================================================
// HAND-CRAFTED REFERENCE IMPLEMENTATION
// =============================================================================
// This file contains the original hand-crafted version of this DTO, created
// before source generation was automated via Inlet.Server.Generators.
//
// Purpose:
// - Serves as a reference implementation to validate generator output
// - Enables test comparisons between generated and expected code
// - Documents the intended structure and behavior of the generated DTO
//
// The generator now produces this DTO automatically. This file is commented out
// to avoid duplicate type definitions but preserved for testing and documentation.
// =============================================================================

#if false
using System.Text.Json.Serialization;

using Mississippi.Inlet.Generators.Abstractions;


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
#endif