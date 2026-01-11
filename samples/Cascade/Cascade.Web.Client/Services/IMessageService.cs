using System;
using System.Threading.Tasks;


namespace Cascade.Web.Client.Services;

/// <summary>
///     Service for sending and receiving real-time messages via SignalR.
/// </summary>
internal interface IMessageService
{
    /// <summary>
    ///     Event raised when a regular message is received.
    /// </summary>
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    ///     Event raised when a greeting is received from the GreeterGrain.
    /// </summary>
    event EventHandler<GreetingReceivedEventArgs>? GreetingReceived;

    /// <summary>
    ///     Event raised when a stream message is received from Orleans streaming.
    /// </summary>
    event EventHandler<StreamMessageReceivedEventArgs>? StreamMessageReceived;

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
    ///     Greets a person via the Orleans grain and broadcasts the result.
    /// </summary>
    /// <param name="name">The name to greet.</param>
    /// <returns>A task representing the async operation.</returns>
    Task GreetAsync(
        string name
    );

    /// <summary>
    ///     Converts input to uppercase via the Orleans grain.
    /// </summary>
    /// <param name="input">The input to convert.</param>
    /// <returns>The uppercase result.</returns>
    Task<string> ToUpperAsync(
        string input
    );

    /// <summary>
    ///     Stops the SignalR connection.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    Task StopAsync();
}
