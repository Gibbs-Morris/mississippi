using Mississippi.Reservoir.Abstractions.Actions;

namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisFormField;

/// <summary>Action dispatched to set the CSS class.</summary>
internal sealed record SetMisFormFieldCssClassAction(string? CssClass) : IAction;
