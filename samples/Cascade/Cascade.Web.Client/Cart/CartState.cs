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
}
