using System;
using System.Threading.Tasks;


namespace Cascade.Web.Client.Services;

/// <summary>
///     Service for sending and receiving real-time messages via SignalR.
/// </summary>
internal interface IMessageService
{
    /// <summary>
    ///     Event raised when a message is received.
    /// </summary>
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    ///     Starts the SignalR connection.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    Task StartAsync();

    /// <summary>
    ///     Sends a message to all connected clients.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SendMessageAsync(
        string message
    );

    /// <summary>
    ///     Stops the SignalR connection.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    Task StopAsync();
}
