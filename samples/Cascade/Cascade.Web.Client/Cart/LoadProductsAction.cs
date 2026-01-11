using Mississippi.Reservoir.Abstractions.Actions;


namespace Cascade.Web.Client.Cart;

/// <summary>
///     Action to request loading available products from the API.
/// </summary>
internal sealed record LoadProductsAction : IAction;