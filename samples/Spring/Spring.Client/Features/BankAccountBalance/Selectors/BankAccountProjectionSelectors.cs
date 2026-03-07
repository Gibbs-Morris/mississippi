using System;

using Mississippi.Inlet.Client.Abstractions.State;

using Spring.Client.Features.BankAccountBalance.Dtos;


namespace Spring.Client.Features.BankAccountBalance.Selectors;

/// <summary>
///     Selectors for deriving values from <see cref="BankAccountBalanceProjectionDto" /> projection state.
/// </summary>
/// <remarks>
///     <para>
///         These selectors follow the factory pattern: each method returns a selector function
///         that closes over the entity ID. This pattern is required for entity-keyed projections
///         because standard selectors only receive the feature state, not additional parameters.
///     </para>
///     <para>
///         Factory selectors enable memoization per entity while keeping selectors pure and testable.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     // In a component:
///     private bool IsAccountOpen => Select(BankAccountProjectionSelectors.IsAccountOpen(SelectedEntityId));
///     </code>
/// </example>
internal static class BankAccountProjectionSelectors
{
    /// <summary>
    ///     Creates a selector that returns the account balance.
    /// </summary>
    /// <param name="entityId">The bank account entity identifier.</param>
    /// <returns>A selector function that returns the balance, or 0 if not loaded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    public static Func<ProjectionsFeatureState, decimal> GetBalance(
        string entityId
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return state =>
        {
            ArgumentNullException.ThrowIfNull(state);
            return state.GetProjection<BankAccountBalanceProjectionDto>(entityId)?.Balance ?? 0m;
        };
    }

    /// <summary>
    ///     Creates a selector that returns any projection error message.
    /// </summary>
    /// <param name="entityId">The bank account entity identifier.</param>
    /// <returns>A selector function that returns the error message, or null if no error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    public static Func<ProjectionsFeatureState, string?> GetErrorMessage(
        string entityId
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return state =>
        {
            ArgumentNullException.ThrowIfNull(state);
            return state.GetProjectionError<BankAccountBalanceProjectionDto>(entityId)?.Message;
        };
    }

    /// <summary>
    ///     Creates a selector that returns the account holder name.
    /// </summary>
    /// <param name="entityId">The bank account entity identifier.</param>
    /// <returns>A selector function that returns the holder name, or null if not loaded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    public static Func<ProjectionsFeatureState, string?> GetHolderName(
        string entityId
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return state =>
        {
            ArgumentNullException.ThrowIfNull(state);
            return state.GetProjection<BankAccountBalanceProjectionDto>(entityId)?.HolderName;
        };
    }

    /// <summary>
    ///     Creates a selector that checks if the projection has an error.
    /// </summary>
    /// <param name="entityId">The bank account entity identifier.</param>
    /// <returns>A selector function that returns true if an error exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    public static Func<ProjectionsFeatureState, bool> HasError(
        string entityId
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return state =>
        {
            ArgumentNullException.ThrowIfNull(state);
            return state.GetProjectionError<BankAccountBalanceProjectionDto>(entityId) is not null;
        };
    }

    /// <summary>
    ///     Creates a selector that checks if the bank account is open.
    /// </summary>
    /// <param name="entityId">The bank account entity identifier.</param>
    /// <returns>A selector function that returns true if the account is open; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    public static Func<ProjectionsFeatureState, bool> IsAccountOpen(
        string entityId
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return state =>
        {
            ArgumentNullException.ThrowIfNull(state);
            return state.GetProjection<BankAccountBalanceProjectionDto>(entityId)?.IsOpen is true;
        };
    }

    /// <summary>
    ///     Creates a selector that checks if the projection is connected to the server.
    /// </summary>
    /// <param name="entityId">The bank account entity identifier.</param>
    /// <returns>A selector function that returns true if connected.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    public static Func<ProjectionsFeatureState, bool> IsConnected(
        string entityId
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return state =>
        {
            ArgumentNullException.ThrowIfNull(state);
            return state.IsProjectionConnected<BankAccountBalanceProjectionDto>(entityId);
        };
    }

    /// <summary>
    ///     Creates a selector that checks if the projection is currently loading.
    /// </summary>
    /// <param name="entityId">The bank account entity identifier.</param>
    /// <returns>A selector function that returns true if loading.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    public static Func<ProjectionsFeatureState, bool> IsLoading(
        string entityId
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return state =>
        {
            ArgumentNullException.ThrowIfNull(state);
            return state.IsProjectionLoading<BankAccountBalanceProjectionDto>(entityId);
        };
    }
}