using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.State;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSwitch;

/// <summary>
///     Feature state for the Kitchen Sink MisSwitch demo.
/// </summary>
internal sealed record MisSwitchKitchenSinkState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "kitchenSink.misSwitch";

    /// <summary>
    ///     Gets the number of events recorded since the last clear.
    /// </summary>
    public int EventCount { get; init; }

    /// <summary>
    ///     Gets the list of emitted interaction event entries.
    /// </summary>
    public IReadOnlyList<string> EventLog { get; init; } = [];

    /// <summary>
    ///     Gets the current switch view model used by the demo component.
    /// </summary>
    public MisSwitchViewModel ViewModel { get; init; } = MisSwitchViewModel.Default with
    {
        IsChecked = true,
        Value = "true",
        IntentId = "kitchen-sink.mis-switch",
        AriaLabel = "Kitchen Sink switch",
        Title = "Kitchen Sink switch",
    };
}