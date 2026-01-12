using System.Collections.Immutable;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Cascade.Client.Cart;

/// <summary>
///     Action dispatched when products have been successfully loaded.
/// </summary>
/// <param name="Products">The list of available products.</param>
internal sealed record ProductsLoadedAction(ImmutableList<string> Products) : IAction;