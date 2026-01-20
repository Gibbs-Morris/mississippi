using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.Greet.Actions;

/// <summary>
///     Action dispatched when a greeting request fails.
/// </summary>
/// <param name="ErrorMessage">The error message.</param>
internal sealed record GreetFailedAction(
    string ErrorMessage
) : IAction;
