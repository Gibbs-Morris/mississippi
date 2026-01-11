using Mississippi.Reservoir.Abstractions.Actions;


namespace Cascade.Web.Client.Cart;

/// <summary>
///     Action to remove an item from the shopping cart.
/// </summary>
/// <param name="ItemId">The unique identifier of the item to remove.</param>
internal sealed record RemoveItemAction(
    string ItemId
) : IAction;
