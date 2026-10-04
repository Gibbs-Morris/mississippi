using Mississippi.Refraction.Client.Infrastructure.Theming;
using Mississippi.Reservoir.Abstractions.Actions;


namespace MississippiSamples.LightSpeed.Client.Features.Showcase;

/// <summary>Selects a local presentation theme.</summary>
/// <param name="Mode">The selected mode.</param>
internal sealed record ChangeThemeAction(RefractionThemeMode Mode) : IAction;