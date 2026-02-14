namespace Mississippi.Refraction.Components.Molecules.MisTextInputActions;

/// <summary>
///     Defines the common contract for text input interaction actions emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisTextInput" />.
/// </summary>
public interface IMisTextInputAction
{
    /// <summary>
    ///     Gets the intent identifier from the text input view model.
    /// </summary>
    string IntentId { get; }
}