using System;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Spring.Client.Features.Greet.Actions;

/// <summary>
///     Action dispatched when a greeting request succeeds.
/// </summary>
/// <param name="Greeting">The greeting message.</param>
/// <param name="GeneratedAt">The timestamp when the greeting was generated.</param>
internal sealed record GreetSucceededAction(string Greeting, DateTime GeneratedAt) : IAction;