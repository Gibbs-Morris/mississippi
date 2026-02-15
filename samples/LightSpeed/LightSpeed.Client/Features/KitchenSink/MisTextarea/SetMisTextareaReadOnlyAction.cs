using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Action dispatched to set whether the textarea is read-only.
/// </summary>
/// <param name="IsReadOnly">A value indicating whether the textarea is read-only.</param>
internal sealed record SetMisTextareaReadOnlyAction(bool IsReadOnly) : IAction;