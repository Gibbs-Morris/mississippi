namespace Mississippi.Refraction.Components.Molecules.MisSwitchActions;

/// <summary>
///     Defines the common contract for switch interaction actions emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisSwitch" />.
/// </summary>
public interface IMisSwitchAction
{
    /// <summary>
    ///     Gets the intent identifier from the switch view model.
    /// </summary>
    string IntentId { get; }
}
