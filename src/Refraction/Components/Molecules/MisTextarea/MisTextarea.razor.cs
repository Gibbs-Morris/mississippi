using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Molecules.MisTextareaActions;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a reusable textarea component for the Mississippi UI library.
/// </summary>
/// <remarks>
///     This component is public so applications referencing the Refraction package can use it directly in markup.
/// </remarks>
public sealed partial class MisTextarea : ComponentBase
{
    private string lastCommittedValue = string.Empty;

    /// <summary>
    ///     Gets or sets additional HTML attributes applied to the root textarea element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the textarea view model.
    /// </summary>
    [Parameter]
    public MisTextareaViewModel Model { get; set; } = MisTextareaViewModel.Default;

    /// <summary>
    ///     Gets or sets the interaction callback for all textarea user actions.
    /// </summary>
    [Parameter]
    public EventCallback<IMisTextareaAction> OnAction { get; set; }

    private string CssClass
    {
        get
        {
            string stateClass = Model.State switch
            {
                MisTextareaState.Default => string.Empty,
                MisTextareaState.Success => "mis-textarea--success",
                MisTextareaState.Warning => "mis-textarea--warning",
                MisTextareaState.Error => "mis-textarea--error",
                var _ => string.Empty,
            };
            string className = "mis-textarea";
            if (!string.IsNullOrWhiteSpace(stateClass))
            {
                className = $"{className} {stateClass}";
            }

            if (!string.IsNullOrWhiteSpace(Model.CssClass))
            {
                className = $"{className} {Model.CssClass}";
            }

            return className;
        }
    }

    private int SafeRows => Model.Rows <= 0 ? 1 : Model.Rows;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        lastCommittedValue = Model.Value ?? string.Empty;
    }

    private Task DispatchActionAsync(
        IMisTextareaAction action
    ) =>
        OnAction.InvokeAsync(action);

    private async Task OnBlurAsync()
    {
        string currentValue = Model.Value ?? string.Empty;
        if (!string.Equals(currentValue, lastCommittedValue, StringComparison.Ordinal))
        {
            await DispatchActionAsync(new MisTextareaChangedAction(Model.IntentId, currentValue));
            lastCommittedValue = currentValue;
        }

        await DispatchActionAsync(new MisTextareaBlurredAction(Model.IntentId));
    }

    private Task OnValueInputAsync(
        string? value
    ) =>
        DispatchActionAsync(new MisTextareaInputAction(Model.IntentId, value ?? string.Empty));
}