using System;


namespace Cascade.Web.Client.Cart;

/// <summary>
///     Contains reducer functions for the shopping cart feature.
/// </summary>
internal static class CartReducers
{
    /// <summary>
    ///     Reducer for adding an item to the cart.
    /// </summary>
    /// <param name="state">The current cart state.</param>
    /// <param name="action">The add item action.</param>
    /// <returns>The new cart state with the item added.</returns>
    public static CartState AddItem(
        CartState state,
        AddItemAction action
    )
    {
        CartItem newItem = new(
            Id: Guid.NewGuid().ToString("N"),
            Name: action.ItemName,
            Quantity: 1);

        return state with { Items = state.Items.Add(newItem) };
    }

    /// <summary>
    ///     Reducer for removing an item from the cart.
    /// </summary>
    /// <param name="state">The current cart state.</param>
    /// <param name="action">The remove item action.</param>
    /// <returns>The new cart state with the item removed.</returns>
    public static CartState RemoveItem(
        CartState state,
        RemoveItemAction action
    ) =>
        state with { Items = state.Items.RemoveAll(item => item.Id == action.ItemId) };
}
