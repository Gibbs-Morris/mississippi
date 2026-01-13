using System;

using Orleans;


namespace Cascade.Grains.Abstractions;

/// <summary>
///     Response from the greeter grain.
/// </summary>
[GenerateSerializer]
[Alias("Cascade.Web.Contracts.GreetResponse")]
public sealed record GreetResult
{
    /// <summary>
    ///     Gets the timestamp when the greeting was generated.
    /// </summary>
    [Id(2)]
    public required DateTime GeneratedAt { get; init; }

    /// <summary>
    ///     Gets the greeting message.
    /// </summary>
    [Id(0)]
    public required string Greeting { get; init; }

    /// <summary>
    ///     Gets the name converted to uppercase.
    /// </summary>
    [Id(1)]
    public required string UppercaseName { get; init; }
}