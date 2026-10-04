using Mississippi.Reservoir.Abstractions.Actions;


namespace MississippiSamples.LightSpeed.Client.Features.Showcase;

/// <summary>Selects local demo completion without starting background work.</summary>
/// <param name="Percent">Completion from zero to 100, or null for unknown duration.</param>
internal sealed record ChangeProgressAction(int? Percent) : IAction;