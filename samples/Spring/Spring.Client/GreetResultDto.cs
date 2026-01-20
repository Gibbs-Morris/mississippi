using System;


namespace Spring.Client;

/// <summary>
///     Response DTO for greet API.
/// </summary>
internal sealed record GreetResultDto
{
    /// <summary>
    ///     Gets the timestamp when the greeting was generated.
    /// </summary>
    public required DateTime GeneratedAt { get; init; }

    /// <summary>
    ///     Gets the greeting message.
    /// </summary>
    public required string Greeting { get; init; }
}