using System;


namespace Cascade.Web.Contracts;

/// <summary>
///     Response from the echo endpoint.
/// </summary>
public sealed record EchoResponse
{
    /// <summary>
    ///     Gets the echoed message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    ///     Gets the time the message was received.
    /// </summary>
    public required DateTime ReceivedAt { get; init; }
}
