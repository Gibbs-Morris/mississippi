namespace Mississippi.Refraction.Components.Molecules.MisRadioGroupActions;

/// <summary>
///     Defines the common contract for radio group interaction actions emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisRadioGroup" />.
/// </summary>
public interface IMisRadioGroupAction
{
    /// <summary>
    ///     Gets the intent identifier from the radio group view model.
    /// </summary>
    string IntentId { get; }
}
