using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.State;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Feature state for the Kitchen Sink MisTextarea demo.
/// </summary>
internal sealed record MisTextareaKitchenSinkState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "kitchenSink.misTextarea";

    /// <summary>
    ///     Gets the number of events recorded since the last clear.
    /// </summary>
    public int EventCount { get; init; }

    /// <summary>
    ///     Gets the list of emitted interaction event entries.
    /// </summary>
    public IReadOnlyList<string> EventLog { get; init; } = [];

    /// <summary>
    ///     Gets the current textarea view model used by the demo component.
    /// </summary>
    public MisTextareaViewModel ViewModel { get; init; } = MisTextareaViewModel.Default with
    {
        Value = "Example multiline text",
        IntentId = "kitchen-sink.mis-textarea",
        AriaLabel = "Kitchen Sink textarea",
        Placeholder = "Type your notes...",
        Title = "Kitchen Sink textarea",
        Rows = 4,
    };
}