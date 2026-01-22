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