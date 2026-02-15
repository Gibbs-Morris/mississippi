namespace Mississippi.Refraction.Components.Molecules.MisTextareaActions;

/// <summary>
///     Represents a change interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisTextarea" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the textarea view model.</param>
/// <param name="Value">The committed textarea value.</param>
public sealed record MisTextareaChangedAction(string IntentId, string Value) : IMisTextareaAction;