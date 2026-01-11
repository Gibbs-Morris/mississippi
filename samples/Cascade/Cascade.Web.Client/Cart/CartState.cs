using System.Collections.Immutable;

using Mississippi.Reservoir.Abstractions.State;


namespace Cascade.Web.Client.Cart;

/// <summary>
///     Represents the shopping cart feature state.
/// </summary>
internal sealed record CartState : IFeatureState
{
    /// <summary>
    ///     Gets the unique key identifying this feature state in the store.
    /// </summary>
    public static string FeatureKey => "cart";

    /// <summary>
    ///     Gets the items currently in the cart.
    /// </summary>
    public ImmutableList<CartItem> Items { get; init; } = [];

    /// <summary>
    ///     Gets the available products loaded from the API.
    /// </summary>
    public ImmutableList<string> AvailableProducts { get; init; } = [];

    /// <summary>
    ///     Gets a value indicating whether products are currently loading.
    /// </summary>
    public bool IsLoadingProducts { get; init; }

    /// <summary>
    ///     Gets the error message if product loading failed.
    /// </summary>
    public string? ProductsError { get; init; }
}
