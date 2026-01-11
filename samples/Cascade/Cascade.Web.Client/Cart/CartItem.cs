namespace Cascade.Web.Client.Cart;

/// <summary>
///     Represents an item in the shopping cart.
/// </summary>
/// <param name="Id">The unique identifier for the cart item.</param>
/// <param name="Name">The display name of the item.</param>
/// <param name="Quantity">The quantity of this item in the cart.</param>
internal sealed record CartItem(
    string Id,
    string Name,
    int Quantity
);
