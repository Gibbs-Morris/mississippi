using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisPasswordInput;

/// <summary>
///     Action to set the password input read-only state.
/// </summary>
/// <param name="IsReadOnly">True to make the input read-only.</param>
public sealed record SetMisPasswordInputReadOnlyAction(bool IsReadOnly) : IAction;