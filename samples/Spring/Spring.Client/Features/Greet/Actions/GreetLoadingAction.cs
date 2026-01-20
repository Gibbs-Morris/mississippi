using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.Greet.Actions;

/// <summary>
///     Action dispatched when a greeting request starts loading.
/// </summary>
internal sealed record GreetLoadingAction : IAction;