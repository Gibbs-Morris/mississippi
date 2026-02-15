using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.SectionUi;

/// <summary>
///     Toggles the events drawer open state for a Kitchen Sink section.
/// </summary>
/// <param name="SectionKey">The unique section key.</param>
internal sealed record ToggleKitchenSinkSectionEventsAction(string SectionKey) : IAction;