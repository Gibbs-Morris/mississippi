using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.State;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;

/// <summary>
///     Feature state for the Kitchen Sink MisCheckbox demo.
/// </summary>
internal sealed record MisCheckboxKitchenSinkState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "kitchenSink.misCheckbox";

    /// <summary>
    ///     Gets the number of events recorded since the last clear.
    /// </summary>
    public int EventCount { get; init; }

    /// <summary>
    ///     Gets the list of emitted interaction event entries.
    /// </summary>
    public IReadOnlyList<string> EventLog { get; init; } = [];

    /// <summary>
    ///     Gets the current checkbox view model used by the demo component.
    /// </summary>
    public MisCheckboxViewModel ViewModel { get; init; } = MisCheckboxViewModel.Default with
    {
        IsChecked = true,
        IntentId = "kitchen-sink.mis-checkbox",
        AriaLabel = "Kitchen Sink checkbox",
        Title = "Kitchen Sink checkbox",
    };
}