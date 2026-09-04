using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

/// <summary>
///     Records local writes with independently controlled connection cancellation and completion.
/// </summary>
internal sealed class RecordingHubConnectionContext : HubConnectionContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RecordingHubConnectionContext" /> class.
    /// </summary>
    /// <param name="connectionId">The unique identifier for the connection.</param>
    /// <param name="writeCompletion">The task controlling when writes complete.</param>
    /// <param name="connectionAborted">The cancellation token representing the connection lifetime.</param>
    public RecordingHubConnectionContext(
        string connectionId,
        Task writeCompletion,
        CancellationToken connectionAborted
    )
        : base(Substitute.For<ConnectionContext>(), new(), NullLoggerFactory.Instance)
    {
        ConnectionAborted = connectionAborted;
        ConnectionId = connectionId;
        WriteCompletion = writeCompletion;
    }

    /// <inheritdoc />
    public override CancellationToken ConnectionAborted { get; }

    /// <inheritdoc />
    public override string ConnectionId { get; }

    /// <summary>
    ///     Gets the most recent message written to the connection.
    /// </summary>
    public HubMessage? LastMessage { get; private set; }

    /// <summary>
    ///     Gets the cancellation token supplied to the most recent write.
    /// </summary>
    public CancellationToken LastWriteCancellationToken { get; private set; }

    private Task WriteCompletion { get; }

    /// <inheritdoc />
    public override ValueTask WriteAsync(
        HubMessage message,
        CancellationToken cancellationToken = default
    )
    {
        LastMessage = message;
        LastWriteCancellationToken = cancellationToken;
        return new(WriteCompletion.WaitAsync(cancellationToken));
    }
}