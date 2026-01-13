using System;


namespace Cascade.Client.Services;

/// <summary>
///     Event arguments for received messages.
/// </summary>
/// <param name="message">The received message.</param>
internal sealed class MessageReceivedEventArgs(string message) : EventArgs
{
    /// <summary>
    ///     Gets the received message.
    /// </summary>
    public string Message { get; } = message;
}