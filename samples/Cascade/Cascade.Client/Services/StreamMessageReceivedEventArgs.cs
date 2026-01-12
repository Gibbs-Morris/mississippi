using System;


namespace Cascade.Client.Services;

/// <summary>
///     Event arguments for received stream messages from Orleans streaming.
/// </summary>
/// <param name="content">The message content.</param>
/// <param name="sender">The sender identifier.</param>
/// <param name="timestamp">The timestamp when the message was sent.</param>
internal sealed class StreamMessageReceivedEventArgs(
    string content,
    string sender,
    DateTimeOffset timestamp
) : EventArgs
{
    /// <summary>
    ///     Gets the message content.
    /// </summary>
    public string Content { get; } = content;

    /// <summary>
    ///     Gets the sender identifier.
    /// </summary>
    public string Sender { get; } = sender;

    /// <summary>
    ///     Gets the timestamp when the message was sent.
    /// </summary>
    public DateTimeOffset Timestamp { get; } = timestamp;
}
