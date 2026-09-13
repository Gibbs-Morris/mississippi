using Mississippi.Reservoir.Abstractions.Actions;


namespace MississippiSamples.LightSpeed.Client.Features.Showcase;

/// <summary>Changes whether the emitter demo accepts activation.</summary>
/// <param name="IsDisabled">Whether activation is disabled.</param>
internal sealed record ChangeEmitterDisabledAction(bool IsDisabled) : IAction;