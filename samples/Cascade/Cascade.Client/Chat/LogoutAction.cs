using Mississippi.Reservoir.Abstractions.Actions;


namespace Cascade.Client.Chat;

/// <summary>
///     Action dispatched when the user logs out.
/// </summary>
internal sealed record LogoutAction : IAction;