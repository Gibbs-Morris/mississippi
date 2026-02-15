using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Action dispatched to set whether the textarea is required.
/// </summary>
/// <param name="IsRequired">A value indicating whether the textarea is required.</param>
internal sealed record SetMisTextareaRequiredAction(bool IsRequired) : IAction;