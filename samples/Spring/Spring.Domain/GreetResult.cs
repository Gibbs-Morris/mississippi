using System;

using Orleans;


namespace Spring.Domain;

/// <summary>
///     Orleans-serializable response from the greeter grain.
/// </summary>
/// <remarks>
///     This type is used for Orleans serialization.
///     The Server maps this to the Client DTO at the API boundary.
/// </remarks>
[GenerateSerializer]
[Alias("Spring.Domain.GreetResult")]
public sealed record GreetResult
{
    /// <summary>
    ///     Gets the timestamp when the greeting was generated.
    /// </summary>
    [Id(1)]
    public required DateTime GeneratedAt { get; init; }

    /// <summary>
    ///     Gets the greeting message.
    /// </summary>
    [Id(0)]
    public required string Greeting { get; init; }
}