using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.State;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckboxGroup;

/// <summary>
///     Feature state for the Kitchen Sink MisCheckboxGroup demo.
/// </summary>
internal sealed record MisCheckboxGroupKitchenSinkState : IFeatureState
{
    private static readonly IReadOnlyList<MisCheckboxOptionViewModel> DefaultOptions =
    [
        new("option1", "Option 1"),
        new("option2", "Option 2"),
        new("option3", "Option 3"),
        new("option4", "Option 4 (Disabled)", true),
    ];

    /// <inheritdoc />
    public static string FeatureKey => "kitchenSink.misCheckboxGroup";

    /// <summary>
    ///     Gets the number of events recorded since the last clear.
    /// </summary>
    public int EventCount { get; init; }

    /// <summary>
    ///     Gets the list of emitted interaction event entries.
    /// </summary>
    public IReadOnlyList<string> EventLog { get; init; } = [];

    /// <summary>
    ///     Gets the current checkbox group view model used by the demo component.
    /// </summary>
    public MisCheckboxGroupViewModel ViewModel { get; init; } = MisCheckboxGroupViewModel.Default with
    {
        IntentId = "kitchen-sink.mis-checkbox-group",
        Options = DefaultOptions,
        Values = new HashSet<string>(),
    };
}