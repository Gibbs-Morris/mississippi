using System;

using Mississippi.Inlet.Projection.Abstractions;


namespace Cascade.Contracts.Projections;

/// <summary>
///     Client DTO for an individual message projection.
/// </summary>
/// <remarks>
///     This represents a single message. Clients subscribe to individual
///     messages based on IDs from <see cref="ChannelMessageIdsDto" />,
///     enabling efficient viewport-based rendering.
/// </remarks>
[ProjectionPath("cascade/messages")]
public sealed record MessageDto
{
    /// <summary>
    ///     Gets the channel this message belongs to.
    /// </summary>
    public required string ChannelId { get; init; }

    /// <summary>
    ///     Gets the message content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was edited, if applicable.
    /// </summary>
    public DateTimeOffset? EditedAt { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the message has been deleted.
    /// </summary>
    public bool IsDeleted { get; init; }

    /// <summary>
    ///     Gets the message identifier.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was sent.
    /// </summary>
    public required DateTimeOffset SentAt { get; init; }

    /// <summary>
    ///     Gets the user ID of the sender.
    /// </summary>
    public required string SentBy { get; init; }
}