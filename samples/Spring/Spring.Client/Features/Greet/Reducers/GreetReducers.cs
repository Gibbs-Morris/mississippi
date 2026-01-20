using Spring.Client.Features.Greet.Actions;
using Spring.Client.Features.Greet.State;


namespace Spring.Client.Features.Greet.Reducers;

/// <summary>
///     Pure reducer functions for the greet feature state.
/// </summary>
internal static class GreetReducers
{
    /// <summary>
    ///     Reduces the <see cref="GreetFailedAction" /> to set error state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing the error message.</param>
    /// <returns>The new state with the error populated.</returns>
    public static GreetState Failed(
        GreetState state,
        GreetFailedAction action
    ) =>
        state with
        {
            IsLoading = false,
            ErrorMessage = action.ErrorMessage,
            Greeting = null,
            GeneratedAt = null,
        };

    /// <summary>
    ///     Reduces the <see cref="GreetLoadingAction" /> to set loading state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <returns>The new state with loading set to true.</returns>
    public static GreetState Loading(
        GreetState state,
        GreetLoadingAction action
    ) =>
        state with
        {
            IsLoading = true,
            ErrorMessage = null,
            Greeting = null,
            GeneratedAt = null,
        };

    /// <summary>
    ///     Reduces the <see cref="GreetSucceededAction" /> to update the greeting.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing the greeting result.</param>
    /// <returns>The new state with the greeting populated.</returns>
    public static GreetState Succeeded(
        GreetState state,
        GreetSucceededAction action
    ) =>
        state with
        {
            IsLoading = false,
            Greeting = action.Greeting,
            GeneratedAt = action.GeneratedAt,
            ErrorMessage = null,
        };
}