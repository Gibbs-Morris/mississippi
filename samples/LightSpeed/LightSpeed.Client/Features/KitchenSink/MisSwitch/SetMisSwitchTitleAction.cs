using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSwitch;

/// <summary>
///     Action dispatched to set the optional switch title.
/// </summary>
/// <param name="Title">The title value.</param>
internal sealed record SetMisSwitchTitleAction(string? Title) : IAction;