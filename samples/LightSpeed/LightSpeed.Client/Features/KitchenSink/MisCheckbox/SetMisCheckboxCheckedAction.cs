using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;

/// <summary>
///     Action dispatched to set whether the checkbox is checked.
/// </summary>
/// <param name="IsChecked">A value indicating whether the checkbox is checked.</param>
internal sealed record SetMisCheckboxCheckedAction(bool IsChecked) : IAction;