using System;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisHelpText;

/// <summary>
///     Reducers for the MisHelpText Kitchen Sink feature state.
/// </summary>
internal static class MisHelpTextKitchenSinkReducers
{
    /// <summary>
    ///     Sets the help text content.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisHelpTextKitchenSinkState SetContent(
        MisHelpTextKitchenSinkState state,
        SetMisHelpTextContentAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        return state with
        {
            HelpTextContent = action.Content ?? "This is helpful instructional text for the form field.",
        };
    }

    /// <summary>
    ///     Sets the optional CSS class.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisHelpTextKitchenSinkState SetCssClass(
        MisHelpTextKitchenSinkState state,
        SetMisHelpTextCssClassAction action
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
    ///     Sets the id attribute.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisHelpTextKitchenSinkState SetId(
        MisHelpTextKitchenSinkState state,
        SetMisHelpTextIdAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        return state with
        {
            ViewModel = state.ViewModel with
            {
                Id = NormalizeOptional(action.Id),
            },
        };
    }

    private static string? NormalizeOptional(
        string? value
    ) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
