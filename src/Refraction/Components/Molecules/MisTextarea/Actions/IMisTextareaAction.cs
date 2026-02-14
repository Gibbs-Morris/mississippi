namespace Mississippi.Refraction.Components.Molecules.MisTextareaActions;

/// <summary>
///     Defines the common contract for textarea interaction actions emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisTextarea" />.
/// </summary>
public interface IMisTextareaAction
{
    /// <summary>
    ///     Gets the intent identifier from the textarea view model.
    /// </summary>
    string IntentId { get; }
}
