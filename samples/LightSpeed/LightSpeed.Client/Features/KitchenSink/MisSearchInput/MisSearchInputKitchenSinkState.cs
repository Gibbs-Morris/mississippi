using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.State;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSearchInput;

/// <summary>
///     Feature state for the Kitchen Sink MisSearchInput demo.
/// </summary>
internal sealed record MisSearchInputKitchenSinkState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "kitchenSink.misSearchInput";

    /// <summary>
    ///     Gets the current search input view model used by the demo component.
    /// </summary>
    public MisSearchInputViewModel ViewModel { get; init; } = MisSearchInputViewModel.Default with
    {
        IntentId = "kitchen-sink.mis-search-input",
        Placeholder = "Search...",
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
