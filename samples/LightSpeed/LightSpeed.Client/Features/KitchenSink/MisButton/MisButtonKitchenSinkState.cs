using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.State;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;

/// <summary>
///     Feature state for the Kitchen Sink MisButton demo.
/// </summary>
internal sealed record MisButtonKitchenSinkState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "kitchenSink.misButton";

    /// <summary>
    ///     Gets the current button view model used by the demo component.
    /// </summary>
    public MisButtonViewModel ViewModel { get; init; } = MisButtonViewModel.Default with
    {
        IntentId = "kitchen-sink.mis-button",
        AriaLabel = "Kitchen Sink button",
        Title = "Kitchen Sink button",
    };

    /// <summary>
    ///     Gets the list of emitted interaction event entries.
    /// </summary>
    public IReadOnlyList<string> EventLog { get; init; } = [];

    /// <summary>
    ///     Gets the number of events recorded since the last clear.
    /// </summary>
    public int EventCount { get; init; }
}