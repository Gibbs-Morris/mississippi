using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Action dispatched to set the textarea row count.
/// </summary>
/// <param name="Rows">The textarea row count.</param>
internal sealed record SetMisTextareaRowsAction(int Rows) : IAction;
