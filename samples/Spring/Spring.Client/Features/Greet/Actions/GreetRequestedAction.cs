using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.Greet.Actions;

/// <summary>
///     Action dispatched when the user requests a greeting.
/// </summary>
/// <param name="Name">The name to greet.</param>
internal sealed record GreetRequestedAction(string Name) : IAction;