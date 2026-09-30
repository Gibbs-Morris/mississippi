using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;


namespace MississippiSamples.LightSpeed.Client.Components.Molecules.Activation;

/// <summary>Demonstrates controlled emitter activation through parent callbacks.</summary>
/// <remarks>Public so pages can compose this presentational molecule without store access.</remarks>
public sealed partial class EmitterDemo : ComponentBase
{
    /// <summary>Gets or sets the activation callback.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> Activated { get; set; }

    /// <summary>Gets or sets the number of recorded activations.</summary>
    [Parameter]
    public int ActivationCount { get; set; }

    /// <summary>Gets or sets the disabled-state callback.</summary>
    [Parameter]
    public EventCallback<bool> DisabledChanged { get; set; }

    /// <summary>Gets or sets a value indicating whether the emitter is disabled.</summary>
    [Parameter]
    public bool IsDisabled { get; set; }

    private string ActivationSummary => $"{ActivationCount} activation{(ActivationCount == 1 ? string.Empty : "s")}";

    private Task HandleDisabledChangedAsync(
        ChangeEventArgs e
    ) =>
        DisabledChanged.InvokeAsync(e.Value is bool value && value);
}