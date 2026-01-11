using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;


namespace Cascade.Web.Server.Hubs;

/// <summary>
///     SignalR hub for broadcasting messages to all connected clients.
/// </summary>
internal sealed class MessageHub : Hub
{
    /// <summary>
    ///     Sends a message to all connected clients.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SendMessageAsync(
        string message
    ) =>
        await Clients.All.SendAsync("ReceiveMessage", message);
}
