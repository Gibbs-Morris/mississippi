using Mississippi.Reservoir.Abstractions.State;


namespace Spring.Client.Features.BankAccountBalance.State;

/// <summary>
///     Feature state for the BankAccountBalance projection.
/// </summary>
/// <remarks>
///     This state holds the read model data fetched from the projection API.
///     It does NOT track command execution - that belongs in the aggregate feature state.
/// </remarks>
internal sealed record BankAccountBalanceState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "bankAccountBalance";

    /// <summary>
    ///     Gets the account ID this projection data belongs to.
    /// </summary>
    public string? AccountId { get; init; }

    /// <summary>
    ///     Gets the current account balance.
    /// </summary>
    public decimal? Balance { get; init; }

    /// <summary>
    ///     Gets the error message from the last failed fetch.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the account holder name.
    /// </summary>
    public string? HolderName { get; init; }

    /// <summary>
    ///     Gets a value indicating whether a fetch is in progress.
    /// </summary>
    public bool IsLoading { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the account is open.
    /// </summary>
    public bool IsOpen { get; init; }
}
