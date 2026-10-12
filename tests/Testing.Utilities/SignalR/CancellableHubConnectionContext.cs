using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;


namespace Mississippi.Testing.Utilities.SignalR;

/// <summary>
///     Hub connection context with a controlled connection-aborted token for testing.
/// </summary>
internal sealed class CancellableHubConnectionContext : HubConnectionContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CancellableHubConnectionContext" /> class.
    /// </summary>
    /// <param name="connectionContext">The connection context to wrap.</param>
    /// <param name="options">The hub connection options.</param>
    /// <param name="connectionAborted">The token returned by <see cref="ConnectionAborted" />.</param>
    public CancellableHubConnectionContext(
        ConnectionContext connectionContext,
        HubConnectionContextOptions options,
        CancellationToken connectionAborted
    )
        : base(connectionContext, options, NullLoggerFactory.Instance) =>
        ConnectionAborted = connectionAborted;

    /// <inheritdoc />
    public override CancellationToken ConnectionAborted { get; }

    /// <inheritdoc />
    public override ValueTask WriteAsync(
        HubMessage message,
        CancellationToken cancellationToken = default
    ) =>
        cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled(cancellationToken) : ValueTask.CompletedTask;
}