using System.Collections.Generic;

using Mississippi.Reservoir.Abstractions.State;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

/// <summary>
///     Feature state for Kitchen Sink section UI state.
/// </summary>
internal sealed record KitchenSinkSectionUiState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "kitchenSink.sectionUi";

    /// <summary>
    ///     Gets the events drawer open state by section key.
    /// </summary>
    public IReadOnlyDictionary<string, bool> EventsPanelOpenStates { get; init; } = new Dictionary<string, bool>();
}