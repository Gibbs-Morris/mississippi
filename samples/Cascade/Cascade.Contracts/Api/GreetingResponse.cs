using System;


namespace Cascade.Contracts.Api;

/// <summary>
///     API response for greeting operations.
/// </summary>
public sealed record GreetingResponse
{
    /// <summary>
    ///     Gets the greeting message.
    /// </summary>
    public required string Greeting { get; init; }

    /// <summary>
    ///     Gets the timestamp when the greeting was generated.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    ///     Gets the name converted to uppercase.
    /// </summary>
    public required string UppercaseName { get; init; }
}
