using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.State;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisRadioGroup;

/// <summary>
///     Feature state for the Kitchen Sink MisRadioGroup demo.
/// </summary>
internal sealed record MisRadioGroupKitchenSinkState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "kitchenSink.misRadioGroup";

    /// <summary>
    ///     Gets the number of events recorded since the last clear.
    /// </summary>
    public int EventCount { get; init; }

    /// <summary>
    ///     Gets the list of emitted interaction event entries.
    /// </summary>
    public IReadOnlyList<string> EventLog { get; init; } = [];

    /// <summary>
    ///     Gets the current radio group view model used by the demo component.
    /// </summary>
    public MisRadioGroupViewModel ViewModel { get; init; } = MisRadioGroupViewModel.Default with
    {
        Value = "pending",
        IntentId = "kitchen-sink.mis-radio-group",
        AriaLabel = "Kitchen Sink radio group",
        Title = "Kitchen Sink radio group",
        Options =
        [
            new("pending", "Pending"),
            new("approved", "Approved"),
            new("rejected", "Rejected"),
            new("archived", "Archived", true),
        ],
    };
}