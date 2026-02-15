namespace Mississippi.Refraction.Components.Molecules.MisPasswordInputActions;

/// <summary>
///     Marker interface for actions emitted by <see cref="MisPasswordInput" />.
/// </summary>
public interface IMisPasswordInputAction
{
    /// <summary>
    ///     Gets the intent identifier emitted back to state handlers.
    /// </summary>
    string IntentId { get; }
}