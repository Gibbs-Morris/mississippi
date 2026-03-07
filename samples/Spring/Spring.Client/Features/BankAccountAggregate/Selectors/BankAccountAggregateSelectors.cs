using System;

using Spring.Client.Features.BankAccountAggregate.State;


namespace Spring.Client.Features.BankAccountAggregate.Selectors;

/// <summary>
///     Selectors for deriving values from <see cref="BankAccountAggregateState" />.
/// </summary>
/// <remarks>
///     <para>
///         These selectors provide a consistent, reusable way to derive values
///         from aggregate command state across components.
///     </para>
/// </remarks>
internal static class BankAccountAggregateSelectors
{
    /// <summary>
    ///     Selects whether the last command succeeded.
    /// </summary>
    /// <param name="state">The aggregate state.</param>
    /// <returns>
    ///     True if the last command succeeded; false if it failed;
    ///     null if no command has completed yet.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static bool? DidLastCommandSucceed(
        BankAccountAggregateState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.LastCommandSucceeded;
    }

    /// <summary>
    ///     Selects the error message from the last failed command.
    /// </summary>
    /// <param name="state">The aggregate state.</param>
    /// <returns>The error message, or null if no error.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static string? GetErrorMessage(
        BankAccountAggregateState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ErrorMessage;
    }

    /// <summary>
    ///     Selects whether any command is currently executing.
    /// </summary>
    /// <param name="state">The aggregate state.</param>
    /// <returns>True if a command is in flight; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static bool IsExecuting(
        BankAccountAggregateState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.IsExecuting;
    }
}