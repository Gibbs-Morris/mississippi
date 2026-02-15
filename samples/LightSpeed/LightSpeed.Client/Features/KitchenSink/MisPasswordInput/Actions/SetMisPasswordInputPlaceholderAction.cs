using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisPasswordInput;

/// <summary>
///     Action to set the password input placeholder text.
/// </summary>
/// <param name="Placeholder">The placeholder text.</param>
public sealed record SetMisPasswordInputPlaceholderAction(string? Placeholder) : IAction;
