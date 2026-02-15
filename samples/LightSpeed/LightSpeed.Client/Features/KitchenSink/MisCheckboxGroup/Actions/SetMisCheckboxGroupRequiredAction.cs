using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckboxGroup;

/// <summary>
///     Action to set the checkbox group required state.
/// </summary>
/// <param name="IsRequired">True to make at least one selection required.</param>
public sealed record SetMisCheckboxGroupRequiredAction(bool IsRequired) : IAction;
