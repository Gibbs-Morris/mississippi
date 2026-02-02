using System;

using Mississippi.Inlet.Client.Abstractions.State;

using Spring.Client.Features.BankAccountAggregate.State;
using Spring.Client.Features.BankAccountBalance.Dtos;


namespace Spring.Client.Features.BankAccountAggregate.Selectors;

/// <summary>
///     Composite selectors that derive values from multiple feature states.
/// </summary>
/// <remarks>
///     <para>
///         These selectors combine aggregate command state with projection state
///         to provide unified views across different state slices.
///     </para>
///     <para>
///         Use composite selectors when the derived value depends on more than one
///         feature state. This centralizes cross-cutting logic and keeps components clean.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     // In a component (using two-state Select overload):
///     private bool IsOperationInProgress => Select(
///         BankAccountCompositeSelectors.IsOperationInProgress(SelectedEntityId));
///     </code>
/// </example>
internal static class BankAccountCompositeSelectors
{
    /// <summary>
    ///     Creates a selector that gets the most relevant error message from either source.
    /// </summary>
    /// <param name="entityId">The bank account entity identifier, or null if no entity selected.</param>
    /// <returns>
    ///     A selector function that returns the aggregate error if present,
    ///     otherwise the projection error, otherwise null.
    /// </returns>
    /// <remarks>
    ///     Aggregate errors take precedence because they represent command failures,
    ///     which are typically more actionable than projection sync errors.
    /// </remarks>
    public static Func<BankAccountAggregateState, ProjectionsFeatureState, string?> GetErrorMessage(
        string? entityId
    ) =>
        (
            aggregateState,
            projectionsState
        ) =>
        {
            ArgumentNullException.ThrowIfNull(aggregateState);
            ArgumentNullException.ThrowIfNull(projectionsState);

            // Aggregate errors take priority (command failures are more actionable)
            if (!string.IsNullOrEmpty(aggregateState.ErrorMessage))
            {
                return aggregateState.ErrorMessage;
            }

            // Fall back to projection errors
            if (!string.IsNullOrEmpty(entityId))
            {
                return projectionsState.GetProjectionError<BankAccountBalanceProjectionDto>(entityId)?.Message;
            }

            return null;
        };

    /// <summary>
    ///     Creates a selector that checks if any operation (command or projection load) is in progress.
    /// </summary>
    /// <param name="entityId">The bank account entity identifier, or null if no entity selected.</param>
    /// <returns>
    ///     A selector function that returns true if a command is executing
    ///     or the projection is loading; otherwise, false.
    /// </returns>
    public static Func<BankAccountAggregateState, ProjectionsFeatureState, bool> IsOperationInProgress(
        string? entityId
    ) =>
        (
            aggregateState,
            projectionsState
        ) =>
        {
            ArgumentNullException.ThrowIfNull(aggregateState);
            ArgumentNullException.ThrowIfNull(projectionsState);
            if (aggregateState.IsExecuting)
            {
                return true;
            }

            if (string.IsNullOrEmpty(entityId))
            {
                return false;
            }

            return projectionsState.IsProjectionLoading<BankAccountBalanceProjectionDto>(entityId);
        };
}