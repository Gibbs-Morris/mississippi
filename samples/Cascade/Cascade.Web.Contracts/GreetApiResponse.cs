using System;


namespace Cascade.Web.Contracts;

/// <summary>
///     API response for greeting operations (client-friendly version without Orleans dependencies).
/// </summary>
public sealed record GreetApiResponse
{
    /// <summary>
    ///     Gets the greeting message.
    /// </summary>
    public required string Greeting { get; init; }

    /// <summary>
    ///     Gets the name converted to uppercase.
    /// </summary>
    public required string UppercaseName { get; init; }

    /// <summary>
    ///     Gets the timestamp when the greeting was generated.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}
