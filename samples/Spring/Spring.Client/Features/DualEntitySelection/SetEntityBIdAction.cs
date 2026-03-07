using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.DualEntitySelection;

/// <summary>
///     Action dispatched to set the account B entity ID.
/// </summary>
/// <param name="EntityId">The entity ID to set, or empty string to clear selection.</param>
internal sealed record SetEntityBIdAction(string EntityId) : IAction;