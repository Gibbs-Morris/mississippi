using System;

using Orleans;


namespace Cascade.Grains.Abstractions;

/// <summary>
///     Message broadcast via Orleans streams.
/// </summary>
[GenerateSerializer]
[Alias("Cascade.Web.Contracts.StreamMessage")]
public sealed record StreamMessage
{
    /// <summary>
    ///     Gets the message content.
    /// </summary>
    [Id(0)]
    public required string Content { get; init; }

    /// <summary>
    ///     Gets the name of the sender.
    /// </summary>
    [Id(1)]
    public required string Sender { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was created.
    /// </summary>
    [Id(2)]
    public required DateTime Timestamp { get; init; }
}
