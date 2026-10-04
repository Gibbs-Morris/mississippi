using Mississippi.Reservoir.Abstractions.Actions;


namespace MississippiSamples.LightSpeed.Client.Features.Showcase;

/// <summary>Changes the local form value.</summary>
/// <param name="Email">The new email address.</param>
internal sealed record ChangeEmailAction(string Email) : IAction;