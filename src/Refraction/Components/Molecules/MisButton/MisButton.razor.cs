using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Components.Molecules.MisButton.Actions;


namespace Mississippi.Refraction.Components.Molecules.MisButton;

/// <summary>
///     Represents a basic reusable button atom for the Mississippi UI library.
/// </summary>
/// <remarks>
///     This component is public so applications referencing the Refraction package can use it directly in markup.
/// </remarks>
public sealed partial class MisButton : ComponentBase
{
    /// <summary>
    ///     Gets or sets additional HTML attributes applied to the root button element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the interaction callback for all button user actions.
    /// </summary>
    [Parameter]
    public EventCallback<IMisButtonAction> OnAction { get; set; }

    /// <summary>
    ///     Gets or sets the button view model.
    /// </summary>
    [Parameter]
    public MisButtonViewModel Model { get; set; } = MisButtonViewModel.Default;

    private string CssClass =>
        string.IsNullOrWhiteSpace(Model.CssClass)
            ? "mis-button"
            : $"mis-button {Model.CssClass}";

    private string ButtonType =>
        Model.Type switch
        {
            MisButtonType.Button => "button",
            MisButtonType.Submit => "submit",
            MisButtonType.Reset => "reset",
            _ => throw new ArgumentOutOfRangeException(nameof(Model.Type), Model.Type, "Unsupported button type."),
        };

    private Task DispatchActionAsync(
        IMisButtonAction action
    ) =>
        OnAction.InvokeAsync(action);

    private Task HandleBlurAsync(
        FocusEventArgs args
    ) =>
        DispatchActionAsync(new MisButtonBlurredAction(Model.IntentId));

    private Task HandleClickAsync(
        MouseEventArgs args
    ) =>
        DispatchActionAsync(new MisButtonClickedAction(
            Model.IntentId,
            args.Button,
            args.CtrlKey,
            args.ShiftKey,
            args.AltKey,
            args.MetaKey));

    private Task HandleMouseDownAsync(
        MouseEventArgs args
    ) =>
        DispatchActionAsync(new MisButtonPointerDownAction(
            Model.IntentId,
            args.Button,
            args.CtrlKey,
            args.ShiftKey,
            args.AltKey,
            args.MetaKey));

    private Task HandleMouseUpAsync(
        MouseEventArgs args
    ) =>
        DispatchActionAsync(new MisButtonPointerUpAction(
            Model.IntentId,
            args.Button,
            args.CtrlKey,
            args.ShiftKey,
            args.AltKey,
            args.MetaKey));

    private Task HandleFocusAsync(
        FocusEventArgs args
    ) =>
        DispatchActionAsync(new MisButtonFocusedAction(Model.IntentId));

    private Task HandleKeyDownAsync(
        KeyboardEventArgs args
    ) =>
        DispatchActionAsync(new MisButtonKeyDownAction(
            Model.IntentId,
            args.Key,
            args.Code,
            args.Repeat,
            args.CtrlKey,
            args.ShiftKey,
            args.AltKey,
            args.MetaKey));

    private Task HandleKeyUpAsync(
        KeyboardEventArgs args
    ) =>
        DispatchActionAsync(new MisButtonKeyUpAction(
            Model.IntentId,
            args.Key,
            args.Code,
            args.CtrlKey,
            args.ShiftKey,
            args.AltKey,
            args.MetaKey));
}
