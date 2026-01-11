using System;


namespace Cascade.Web.Client.Services;

/// <summary>
///     Event arguments for received stream messages from Orleans streaming.
/// </summary>
internal sealed class StreamMessageReceivedEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="StreamMessageReceivedEventArgs" /> class.
    /// </summary>
    /// <param name="content">The message content.</param>
    /// <param name="sender">The sender identifier.</param>
    /// <param name="timestamp">The timestamp when the message was sent.</param>
    public StreamMessageReceivedEventArgs(
        string content,
        string sender,
        DateTimeOffset timestamp
    )
    {
        Content = content;
        Sender = sender;
        Timestamp = timestamp;
    }

    /// <summary>
    ///     Gets the message content.
    /// </summary>
    public string Content { get; }

    /// <summary>
    ///     Gets the sender identifier.
    /// </summary>
    public string Sender { get; }

    /// <summary>
    ///     Gets the timestamp when the message was sent.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
