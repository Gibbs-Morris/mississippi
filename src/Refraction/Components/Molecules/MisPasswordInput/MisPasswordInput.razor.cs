using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Molecules.MisPasswordInputActions;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a password input component with show/hide toggle.
/// </summary>
/// <remarks>
///     This component is public so applications referencing the Refraction package can use it directly in markup.
/// </remarks>
public sealed partial class MisPasswordInput : ComponentBase
{
    private string lastCommittedValue = string.Empty;

    /// <summary>
    ///     Gets or sets additional HTML attributes applied to the input element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the password input view model.
    /// </summary>
    [Parameter]
    public MisPasswordInputViewModel Model { get; set; } = MisPasswordInputViewModel.Default;

    /// <summary>
    ///     Gets or sets the interaction callback for all password input user actions.
    /// </summary>
    [Parameter]
    public EventCallback<IMisPasswordInputAction> OnAction { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        lastCommittedValue = Model.Value ?? string.Empty;
    }

    private string ContainerCssClass
    {
        get
        {
            List<string> classes = ["mis-password-input"];

            string stateClass = Model.State switch
            {
                MisPasswordInputState.Error => "mis-password-input--error",
                MisPasswordInputState.Warning => "mis-password-input--warning",
                MisPasswordInputState.Success => "mis-password-input--success",
                _ => string.Empty,
            };

            if (!string.IsNullOrWhiteSpace(stateClass))
            {
                classes.Add(stateClass);
            }

            if (Model.IsDisabled)
            {
                classes.Add("mis-password-input--disabled");
            }

            if (!string.IsNullOrWhiteSpace(Model.CssClass))
            {
                classes.Add(Model.CssClass);
            }

            return string.Join(" ", classes);
        }
    }

    private static string InputCssClass => "mis-password-input__input";

    private string InputType => Model.IsPasswordVisible ? "text" : "password";

    private string ToggleAriaLabel => Model.IsPasswordVisible ? "Hide password" : "Show password";

    private async Task OnBlurAsync()
    {
        string currentValue = Model.Value ?? string.Empty;

        if (!string.Equals(currentValue, lastCommittedValue, System.StringComparison.Ordinal))
        {
            await DispatchActionAsync(new MisPasswordInputChangedAction(Model.IntentId, currentValue));
            lastCommittedValue = currentValue;
        }

        await DispatchActionAsync(new MisPasswordInputBlurredAction(Model.IntentId));
    }

    private Task DispatchActionAsync(
        IMisPasswordInputAction action
    ) =>
        OnAction.InvokeAsync(action);
}
