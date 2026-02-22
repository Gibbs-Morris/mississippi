using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;


namespace Mississippi.Aqueduct;

/// <summary>
///     Sends SignalR invocation messages to local hub connections.
/// </summary>
/// <remarks>
///     <para>
///         This implementation creates an <see cref="InvocationMessage" /> and
///         writes it to the connection's channel.
///     </para>
/// </remarks>
internal sealed class LocalMessageSender : ILocalMessageSender
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="LocalMessageSender" /> class.
    /// </summary>
    /// <param name="logger">Logger instance for message-sending operations.</param>
    public LocalMessageSender(
        ILogger<LocalMessageSender> logger
    ) =>
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private ILogger<LocalMessageSender> Logger { get; }

    /// <inheritdoc />
    public async Task SendAsync(
        HubConnectionContext connection,
        string methodName,
        IReadOnlyList<object?> args
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrEmpty(methodName);
        Logger.SendingLocalMessage(connection.ConnectionId, methodName);
        object?[] argsArray = args as object?[] ?? args.ToArray();
        InvocationMessage invocation = new(methodName, argsArray);
        await connection.WriteAsync(invocation).ConfigureAwait(false);
    }
}