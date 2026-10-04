using System;


namespace MississippiSamples.LightSpeed.Client.Features.Showcase;

/// <summary>Applies local showcase intent without side effects.</summary>
internal static class ShowcaseReducers
{
    /// <summary>Records a presentational emitter activation.</summary>
    /// <param name="state">Current state.</param>
    /// <param name="action">The activation.</param>
    /// <returns>A new state.</returns>
    public static ShowcaseState ActivateEmitter(
        ShowcaseState state,
        ActivateEmitterAction action
    ) =>
        state with
        {
            EmitterActivationCount = state.EmitterActivationCount + 1,
            ActionCount = state.ActionCount + 1,
            LastAction = nameof(ActivateEmitterAction),
        };

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

    /// <summary>Changes whether the presentational emitter accepts activation.</summary>
    /// <param name="state">Current state.</param>
    /// <param name="action">The disabled-state change.</param>
    /// <returns>A new state.</returns>
    public static ShowcaseState ChangeEmitterDisabled(
        ShowcaseState state,
        ChangeEmitterDisabledAction action
    ) =>
        state with
        {
            IsEmitterDisabled = action.IsDisabled,
            ActionCount = state.ActionCount + 1,
            LastAction = nameof(ChangeEmitterDisabledAction),
        };

    /// <summary>Changes the demo completion without affecting the form.</summary>
    /// <param name="state">Current state.</param>
    /// <param name="action">The selection.</param>
    /// <returns>Updated state, or the original state for an unsupported percentage.</returns>
    public static ShowcaseState ChangeProgress(
        ShowcaseState state,
        ChangeProgressAction action
    ) =>
        action.Percent is < 0 or > 100
            ? state
            : state with
            {
                ProgressPercent = action.Percent,
                ActionCount = state.ActionCount + 1,
                LastAction = nameof(ChangeProgressAction),
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

    /// <summary>Hides and collapses the visible notification.</summary>
    /// <param name="state">Current state.</param>
    /// <param name="action">The dismissal request.</param>
    /// <returns>The updated state, or the original state when hidden.</returns>
    public static ShowcaseState DismissNotification(
        ShowcaseState state,
        DismissNotificationAction action
    ) =>
        !state.IsNotificationVisible
            ? state
            : state with
            {
                IsNotificationVisible = false,
                IsNotificationExpanded = false,
                ActionCount = state.ActionCount + 1,
                LastAction = nameof(DismissNotificationAction),
            };

    /// <summary>Expands the notification while it remains visible.</summary>
    /// <param name="state">Current state.</param>
    /// <param name="action">The expansion request.</param>
    /// <returns>The updated state, or the original state when hidden.</returns>
    public static ShowcaseState ExpandNotification(
        ShowcaseState state,
        ExpandNotificationAction action
    ) =>
        !state.IsNotificationVisible
            ? state
            : state with
            {
                IsNotificationExpanded = true,
                ActionCount = state.ActionCount + 1,
                LastAction = nameof(ExpandNotificationAction),
            };

    /// <summary>Resets the form, preserves selected demos, and advances the action count.</summary>
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
            ProgressPercent = state.ProgressPercent,
            EmitterActivationCount = state.EmitterActivationCount,
            IsEmitterDisabled = state.IsEmitterDisabled,
            IsNotificationVisible = state.IsNotificationVisible,
            IsNotificationExpanded = state.IsNotificationExpanded,
            ActionCount = state.ActionCount + 1,
            LastAction = nameof(ResetProfileAction),
        };

    /// <summary>Restores the notification in its collapsed state.</summary>
    /// <param name="state">Current state.</param>
    /// <param name="action">The restoration request.</param>
    /// <returns>The updated state, or the original state when already visible.</returns>
    public static ShowcaseState RestoreNotification(
        ShowcaseState state,
        RestoreNotificationAction action
    ) =>
        state.IsNotificationVisible
            ? state
            : state with
            {
                IsNotificationVisible = true,
                IsNotificationExpanded = false,
                ActionCount = state.ActionCount + 1,
                LastAction = nameof(RestoreNotificationAction),
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