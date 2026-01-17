using Mississippi.Reservoir.Abstractions.Actions;


namespace Cascade.Client.Chat;

/// <summary>
///     Action dispatched when login fails.
/// </summary>
/// <param name="Error">The error message.</param>
internal sealed record LoginFailedAction(string Error) : IAction;