using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client;


namespace MississippiSamples.LightSpeed.Client.Components.Molecules.Progress;

/// <summary>Demonstrates controlled progress states through parent callbacks.</summary>
/// <remarks>Public so Razor pages can compose this presentational molecule.</remarks>
public sealed partial class ProgressDemo : ComponentBase
{
    private static int[] CompletionOptions { get; } = [0, 25, 50, 75, 100];

    /// <summary>Gets or sets completion, or null for unknown duration.</summary>
    [Parameter]
    public int? Percent { get; set; }

    /// <summary>Gets or sets the selection callback.</summary>
    [Parameter]
    public EventCallback<int?> PercentChanged { get; set; }

    private string ProgressState => Percent is null ? RefractionStates.Indeterminate : RefractionStates.Determinate;

    private string StatusText => Percent is null ? "Completion unknown" : $"{Percent}% complete";
}