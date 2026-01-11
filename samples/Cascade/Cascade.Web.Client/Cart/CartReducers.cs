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

    /// <summary>
    ///     Reducer for setting loading state when products are being fetched.
    /// </summary>
    /// <param name="state">The current cart state.</param>
    /// <param name="action">The products loading action.</param>
    /// <returns>The new cart state with loading indicator set.</returns>
    public static CartState ProductsLoading(
        CartState state,
        ProductsLoadingAction action
    ) =>
        state with { IsLoadingProducts = true, ProductsError = null };

    /// <summary>
    ///     Reducer for setting products when they have been loaded.
    /// </summary>
    /// <param name="state">The current cart state.</param>
    /// <param name="action">The products loaded action.</param>
    /// <returns>The new cart state with products populated.</returns>
    public static CartState ProductsLoaded(
        CartState state,
        ProductsLoadedAction action
    ) =>
        state with { IsLoadingProducts = false, AvailableProducts = action.Products, ProductsError = null };

    /// <summary>
    ///     Reducer for handling product load failures.
    /// </summary>
    /// <param name="state">The current cart state.</param>
    /// <param name="action">The products load failed action.</param>
    /// <returns>The new cart state with error information.</returns>
    public static CartState ProductsLoadFailed(
        CartState state,
        ProductsLoadFailedAction action
    ) =>
        state with { IsLoadingProducts = false, ProductsError = action.Error };
}
