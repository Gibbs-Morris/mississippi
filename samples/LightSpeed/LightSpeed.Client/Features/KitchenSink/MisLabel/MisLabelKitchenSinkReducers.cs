using System;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisLabel;

/// <summary>
///     Reducers for the MisLabel Kitchen Sink feature state.
/// </summary>
internal static class MisLabelKitchenSinkReducers
{
    /// <summary>
    ///     Sets the optional CSS class.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisLabelKitchenSinkState SetCssClass(
        MisLabelKitchenSinkState state,
        SetMisLabelCssClassAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                CssClass = NormalizeOptional(action.CssClass),
            },
        };
    }

    /// <summary>
    ///     Sets the 'for' attribute value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisLabelKitchenSinkState SetFor(
        MisLabelKitchenSinkState state,
        SetMisLabelForAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                For = NormalizeOptional(action.For),
            },
        };
    }

    /// <summary>
    ///     Sets whether the label indicates a required field.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisLabelKitchenSinkState SetIsRequired(
        MisLabelKitchenSinkState state,
        SetMisLabelIsRequiredAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                IsRequired = action.IsRequired,
            },
        };
    }

    /// <summary>
    ///     Sets the visual state of the label.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisLabelKitchenSinkState SetState(
        MisLabelKitchenSinkState state,
        SetMisLabelStateAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                State = action.State,
            },
        };
    }

    /// <summary>
    ///     Sets the label text content.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisLabelKitchenSinkState SetText(
        MisLabelKitchenSinkState state,
        SetMisLabelTextAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            LabelText = action.Text ?? "Demo Label",
        };
    }

    private static string? NormalizeOptional(
        string? value
    ) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}