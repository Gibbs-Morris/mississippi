namespace Mississippi.Refraction.Components.Molecules.MisTextareaActions;

/// <summary>
///     Represents a pointer down interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisTextarea" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the textarea view model.</param>
/// <param name="Button">The numeric mouse button code from the pointer event.</param>
/// <param name="CtrlKey">A value indicating whether the Ctrl key was pressed.</param>
/// <param name="ShiftKey">A value indicating whether the Shift key was pressed.</param>
/// <param name="AltKey">A value indicating whether the Alt key was pressed.</param>
/// <param name="MetaKey">A value indicating whether the Meta key was pressed.</param>
public sealed record MisTextareaPointerDownAction(
    string IntentId,
    long Button,
    bool CtrlKey,
    bool ShiftKey,
    bool AltKey,
    bool MetaKey
) : IMisTextareaAction;