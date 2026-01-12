using System;


namespace Cascade.Client.Services;

/// <summary>
///     Event arguments for received messages.
/// </summary>
internal sealed class MessageReceivedEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageReceivedEventArgs" /> class.
    /// </summary>
    /// <param name="message">The received message.</param>
    public MessageReceivedEventArgs(
        string message
    ) =>
        Message = message;

    /// <summary>
    ///     Gets the received message.
    /// </summary>
    public string Message { get; }
}