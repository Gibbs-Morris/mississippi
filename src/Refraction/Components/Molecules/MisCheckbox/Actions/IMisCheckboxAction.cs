namespace Mississippi.Refraction.Components.Molecules.MisCheckboxActions;

/// <summary>
///     Defines the common contract for checkbox interaction actions emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisCheckbox" />.
/// </summary>
public interface IMisCheckboxAction
{
    /// <summary>
    ///     Gets the intent identifier from the checkbox view model.
    /// </summary>
    string IntentId { get; }
}