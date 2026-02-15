namespace Mississippi.Refraction.Components.Molecules.MisTextareaActions;

/// <summary>
///     Represents an input interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisTextarea" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the textarea view model.</param>
/// <param name="Value">The current textarea value.</param>
public sealed record MisTextareaInputAction(string IntentId, string Value) : IMisTextareaAction;