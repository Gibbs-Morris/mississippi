using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisPasswordInput;

/// <summary>
///     Action to set the password input value.
/// </summary>
/// <param name="Value">The new value.</param>
public sealed record SetMisPasswordInputValueAction(string? Value) : IAction;
