namespace Mississippi.Refraction.Components.Molecules.MisTextareaActions;

/// <summary>
///     Represents a blur interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisTextarea" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the textarea view model.</param>
public sealed record MisTextareaBlurredAction(
    string IntentId
) : IMisTextareaAction;
