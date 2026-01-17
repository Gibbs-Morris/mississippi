using Mississippi.Reservoir.Abstractions.Actions;


namespace Cascade.Client.Chat;

/// <summary>
///     Action dispatched when login completes successfully.
/// </summary>
/// <param name="UserId">The authenticated user's ID.</param>
/// <param name="DisplayName">The authenticated user's display name.</param>
internal sealed record LoginSuccessAction(string UserId, string DisplayName) : IAction;