namespace Mississippi.Refraction.Components.Molecules.MisSelectActions;

/// <summary>
///     Defines the common contract for select interaction actions emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisSelect" />.
/// </summary>
public interface IMisSelectAction
{
    /// <summary>
    ///     Gets the intent identifier from the select view model.
    /// </summary>
    string IntentId { get; }
}