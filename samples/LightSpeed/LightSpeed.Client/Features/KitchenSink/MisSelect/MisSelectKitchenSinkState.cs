using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.State;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;

/// <summary>
///     Feature state for the Kitchen Sink MisSelect demo.
/// </summary>
internal sealed record MisSelectKitchenSinkState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "kitchenSink.misSelect";

    /// <summary>
    ///     Gets the current select view model used by the demo component.
    /// </summary>
    public MisSelectViewModel ViewModel { get; init; } = MisSelectViewModel.Default with
    {
        Value = string.Empty,
        IntentId = "kitchen-sink.mis-select",
        AriaLabel = "Kitchen Sink select",
        Placeholder = "Select a value",
        Title = "Kitchen Sink select",
        Options =
        [
            new MisSelectOptionViewModel("pending", "Pending"),
            new MisSelectOptionViewModel("approved", "Approved"),
            new MisSelectOptionViewModel("rejected", "Rejected"),
            new MisSelectOptionViewModel("archived", "Archived", true),
        ],
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