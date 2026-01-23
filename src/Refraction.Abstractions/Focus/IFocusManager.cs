namespace Mississippi.Refraction.Abstractions.Focus;

/// <summary>
///     Manages focus tracking and transitions within Refraction components.
/// </summary>
/// <remarks>
///     The focus manager coordinates keyboard navigation, focus traps,
///     and announces focus changes to parent components.
/// </remarks>
public interface IFocusManager
{
    /// <summary>
    ///     Gets the identifier of the currently focused element within the scope.
    /// </summary>
    string? CurrentFocusId { get; }

    /// <summary>
    ///     Clears the current focus within the scope.
    /// </summary>
    void ClearFocus();

    /// <summary>
    ///     Moves focus to the next focusable element in tab order.
    /// </summary>
    void FocusNext();

    /// <summary>
    ///     Moves focus to the previous focusable element in tab order.
    /// </summary>
    void FocusPrevious();

    /// <summary>
    ///     Moves focus to the specified element.
    /// </summary>
    /// <param name="elementId">The id of the element to focus.</param>
    /// <returns>True if focus was successfully moved; false if the element was not found.</returns>
    bool TryFocus(
        string elementId
    );
}