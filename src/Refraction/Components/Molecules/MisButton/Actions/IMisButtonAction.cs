namespace Mississippi.Refraction.Components.Molecules.MisButtonActions;

/// <summary>
///     Defines the common contract for button interaction actions emitted by
///     <see cref="MisButton" />.
/// </summary>
public interface IMisButtonAction
{
    /// <summary>
    ///     Gets the intent identifier from the button view model.
    /// </summary>
    string IntentId { get; }
}