using Mississippi.Reservoir.Abstractions.Actions;


namespace Cascade.Client.Cart;

/// <summary>
///     Action dispatched when products are loading.
/// </summary>
internal sealed record ProductsLoadingAction : IAction;