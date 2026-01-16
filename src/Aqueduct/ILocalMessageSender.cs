using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;


namespace Mississippi.Aqueduct;

/// <summary>
///     Defines a service for sending SignalR messages to local connections.
/// </summary>
/// <remarks>
///     <para>
///         This interface abstracts the mechanics of writing SignalR invocation
///         messages to a <see cref="HubConnectionContext" />. By isolating this
///         concern, the message-sending logic can be easily tested and mocked.
///     </para>
///     <para>
///         Implementations should handle message serialization via the SignalR
///         protocol and write to the connection's channel.
///     </para>
/// </remarks>
public interface ILocalMessageSender
{
    /// <summary>
    ///     Sends an invocation message to a local SignalR connection.
    /// </summary>
    /// <param name="connection">The target hub connection context.</param>
    /// <param name="methodName">The name of the hub method to invoke on the client.</param>
    /// <param name="args">The arguments to pass to the hub method.</param>
    /// <returns>A task representing the asynchronous send operation.</returns>
    Task SendAsync(
        HubConnectionContext connection,
        string methodName,
        IReadOnlyList<object?> args
    );
}
