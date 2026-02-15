using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Components.Molecules.MisSearchInputActions;


namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a search input component with search icon and clear button.
/// </summary>
/// <remarks>
///     This component is public so applications referencing the Refraction package can use it directly in markup.
/// </remarks>
public sealed partial class MisSearchInput : ComponentBase
{
    private string lastCommittedValue = string.Empty;

    /// <summary>
    ///     Gets or sets additional HTML attributes applied to the input element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the search input view model.
    /// </summary>
    [Parameter]
    public MisSearchInputViewModel Model { get; set; } = MisSearchInputViewModel.Default;

    /// <summary>
    ///     Gets or sets the interaction callback for all search input user actions.
    /// </summary>
    [Parameter]
    public EventCallback<IMisSearchInputAction> OnAction { get; set; }

    private string ContainerCssClass
    {
        get
        {
            List<string> classes = ["mis-search-input"];
            if (Model.IsDisabled)
            {
                classes.Add("mis-search-input--disabled");
            }

            if (!string.IsNullOrWhiteSpace(Model.CssClass))
            {
                classes.Add(Model.CssClass);
            }

            return string.Join(" ", classes);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        lastCommittedValue = Model.Value ?? string.Empty;
    }

    private Task DispatchActionAsync(
        IMisSearchInputAction action
    ) =>
        OnAction.InvokeAsync(action);

    private Task HandleKeyDownAsync(
        KeyboardEventArgs args
    )
    {
        if (args.Key == "Enter")
        {
            return DispatchActionAsync(new MisSearchInputSubmittedAction(Model.IntentId, Model.Value));
        }

        return Task.CompletedTask;
    }

    private async Task OnBlurAsync()
    {
        string currentValue = Model.Value ?? string.Empty;
        if (!string.Equals(currentValue, lastCommittedValue, StringComparison.Ordinal))
        {
            await DispatchActionAsync(new MisSearchInputChangedAction(Model.IntentId, currentValue));
            lastCommittedValue = currentValue;
        }

        await DispatchActionAsync(new MisSearchInputBlurredAction(Model.IntentId));
    }

    private Task OnValueInputAsync(
        string? value
    ) =>
        DispatchActionAsync(new MisSearchInputInputAction(Model.IntentId, value ?? string.Empty));
}