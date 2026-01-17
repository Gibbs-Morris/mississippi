using Mississippi.Reservoir.Abstractions.Actions;


namespace Cascade.Client.Cart;

/// <summary>
///     Action dispatched when product loading fails.
/// </summary>
/// <param name="Error">The error message.</param>
internal sealed record ProductsLoadFailedAction(string Error) : IAction;