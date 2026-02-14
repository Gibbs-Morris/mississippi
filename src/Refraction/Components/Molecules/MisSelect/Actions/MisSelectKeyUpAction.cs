namespace Mississippi.Refraction.Components.Molecules.MisSelectActions;

/// <summary>
///     Represents a key up interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisSelect" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the select view model.</param>
/// <param name="Key">The key associated with the keyboard event.</param>
/// <param name="Code">The physical key code from the keyboard event.</param>
/// <param name="CtrlKey">A value indicating whether the Ctrl key was pressed.</param>
/// <param name="ShiftKey">A value indicating whether the Shift key was pressed.</param>
/// <param name="AltKey">A value indicating whether the Alt key was pressed.</param>
/// <param name="MetaKey">A value indicating whether the Meta key was pressed.</param>
public sealed record MisSelectKeyUpAction(
    string IntentId,
    string Key,
    string Code,
    bool CtrlKey,
    bool ShiftKey,
    bool AltKey,
    bool MetaKey
) : IMisSelectAction;