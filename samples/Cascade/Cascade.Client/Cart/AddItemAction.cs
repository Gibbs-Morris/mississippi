using Mississippi.Reservoir.Abstractions.Actions;


namespace Cascade.Client.Cart;

/// <summary>
///     Action to add an item to the shopping cart.
/// </summary>
/// <param name="ItemName">The name of the item to add.</param>
internal sealed record AddItemAction(string ItemName) : IAction;