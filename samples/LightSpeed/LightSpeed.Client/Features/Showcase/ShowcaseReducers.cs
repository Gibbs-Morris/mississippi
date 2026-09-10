using System;


namespace MississippiSamples.LightSpeed.Client.Features.Showcase;

/// <summary>Applies local showcase intent without side effects.</summary>
internal static class ShowcaseReducers
{
    /// <summary>Changes the email value.</summary>
    /// <param name="state">Current state.</param>
    /// <param name="action">The edit.</param>
    /// <returns>A new state.</returns>
    public static ShowcaseState ChangeEmail(
        ShowcaseState state,
        ChangeEmailAction action
    ) =>
        state with
        {
            Email = action.Email,
            ActionCount = state.ActionCount + 1,
            LastAction = nameof(ChangeEmailAction),
        };

    /// <summary>Changes to a supported theme.</summary>
    /// <param name="state">Current state.</param>
    /// <param name="action">The selection.</param>
    /// <returns>The updated state, or the original state for an unsupported mode.</returns>
    public static ShowcaseState ChangeTheme(
        ShowcaseState state,
        ChangeThemeAction action
    ) =>
        Enum.IsDefined(action.Mode)
            ? state with
            {
                ThemeMode = action.Mode,
                ActionCount = state.ActionCount + 1,
                LastAction = nameof(ChangeThemeAction),
            }
            : state;

    /// <summary>Resets the form while retaining its theme and action history count.</summary>
    /// <param name="state">Current state.</param>
    /// <param name="action">The request.</param>
    /// <returns>A reset state.</returns>
    public static ShowcaseState Reset(
        ShowcaseState state,
        ResetProfileAction action
    ) =>
        new()
        {
            ThemeMode = state.ThemeMode,
            ActionCount = state.ActionCount + 1,
            LastAction = nameof(ResetProfileAction),
        };

    /// <summary>Requests form validation.</summary>
    /// <param name="state">Current state.</param>
    /// <param name="action">The request.</param>
    /// <returns>A new state.</returns>
    public static ShowcaseState Validate(
        ShowcaseState state,
        ValidateProfileAction action
    ) =>
        state with
        {
            IsSubmitted = true,
            ActionCount = state.ActionCount + 1,
            LastAction = nameof(ValidateProfileAction),
        };
}