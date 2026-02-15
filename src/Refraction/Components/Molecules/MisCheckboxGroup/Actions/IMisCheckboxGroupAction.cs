namespace Mississippi.Refraction.Components.Molecules.MisCheckboxGroupActions;

/// <summary>
///     Marker interface for actions emitted by <see cref="MisCheckboxGroup" />.
/// </summary>
public interface IMisCheckboxGroupAction
{
    /// <summary>
    ///     Gets the intent identifier emitted back to state handlers.
    /// </summary>
    string IntentId { get; }
}
