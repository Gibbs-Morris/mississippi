namespace Mississippi.Refraction.Components.Molecules.MisSearchInputActions;

/// <summary>
///     Marker interface for actions emitted by <see cref="MisSearchInput" />.
/// </summary>
public interface IMisSearchInputAction
{
    /// <summary>
    ///     Gets the intent identifier emitted back to state handlers.
    /// </summary>
    string IntentId { get; }
}
