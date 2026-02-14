using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.State;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

/// <summary>
///     Feature state for the Kitchen Sink MisTextInput demo.
/// </summary>
internal sealed record MisTextInputKitchenSinkState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "kitchenSink.misTextInput";

    /// <summary>
    ///     Gets the current text input view model used by the demo component.
    /// </summary>
    public MisTextInputViewModel ViewModel { get; init; } = MisTextInputViewModel.Default with
    {
        Value = "Hello from Kitchen Sink",
        IntentId = "kitchen-sink.mis-text-input",
        AriaLabel = "Kitchen Sink text input",
        Placeholder = "Type here",
        Title = "Kitchen Sink text input",
        AutoComplete = "off",
        Type = MisTextInputType.Text,
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
