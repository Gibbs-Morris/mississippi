using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisPasswordInput;

/// <summary>
///     Action to set the password visibility state.
/// </summary>
/// <param name="IsVisible">True to show password, false to hide.</param>
public sealed record SetMisPasswordInputVisibilityAction(bool IsVisible) : IAction;
