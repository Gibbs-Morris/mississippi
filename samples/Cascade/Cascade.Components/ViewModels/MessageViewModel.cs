using System;


namespace Cascade.Components.ViewModels;

/// <summary>
///     View model for displaying a message in the UI.
/// </summary>
public sealed record MessageViewModel
{
    /// <summary>
    ///     Gets the display name of the message author.
    /// </summary>
    public required string AuthorDisplayName { get; init; }

    /// <summary>
    ///     Gets the user ID of the author.
    /// </summary>
    public required string AuthorUserId { get; init; }

    /// <summary>
    ///     Gets the message content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the message has been deleted.
    /// </summary>
    public bool IsDeleted { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the message has been edited.
    /// </summary>
    public bool IsEdited { get; init; }

    /// <summary>
    ///     Gets the message identifier.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was sent.
    /// </summary>
    public required DateTimeOffset SentAt { get; init; }
}
