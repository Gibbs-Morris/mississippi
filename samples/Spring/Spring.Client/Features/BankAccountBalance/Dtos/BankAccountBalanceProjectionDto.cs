#if FALSE // Replaced by ProjectionClientDtoGenerator - keep for reference
// =============================================================================
// HAND-CRAFTED REFERENCE IMPLEMENTATION
// =============================================================================
// This file contains the original hand-crafted version of this type, created
// before source generation was automated via Sdk.Client.Generators.
//
// Purpose:
// - Serves as a reference implementation to validate generator output
// - Enables test comparisons between generated and expected code
// - Documents the intended structure and behavior of the generated type
//
// The generator now produces this DTO automatically. This file is commented out
// to avoid duplicate type definitions but preserved for testing and documentation.
// =============================================================================
using Mississippi.Inlet.Projection.Abstractions;
using Mississippi.Sdk.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountBalance.Dtos;

/// <summary>
///     Client-side DTO for the bank account balance projection.
/// </summary>
/// <remarks>
///     <para>
///         This DTO mirrors the server-side <c>BankAccountBalanceProjection</c> and is used
///         by Inlet to automatically fetch and cache projection data via SignalR.
///     </para>
///     <para>
///         The <see cref="ProjectionPathAttribute" /> must match the server projection's path
///         for Inlet to correctly route subscription requests.
///     </para>
/// </remarks>
[PendingSourceGenerator]
[ProjectionPath("bank-account-balance")]
public sealed record BankAccountBalanceProjectionDto
{
    /// <summary>
    ///     Gets the current balance of the account.
    /// </summary>
    public required decimal Balance { get; init; }

    /// <summary>
    ///     Gets the account holder name.
    /// </summary>
    public required string HolderName { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the account is open.
    /// </summary>
    public required bool IsOpen { get; init; }
}
#endif

