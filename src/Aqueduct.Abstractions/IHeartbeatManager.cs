using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Aqueduct.Abstractions;

/// <summary>
///     Defines a service for managing server heartbeat operations.
/// </summary>
/// <remarks>
///     <para>
///         The heartbeat manager is responsible for periodically reporting
///         server liveness and connection counts to the server directory grain.
///         This enables dead server detection and cleanup of orphaned connections.
///     </para>
///     <para>
///         Implementations should handle timer-based heartbeat scheduling and
///         graceful shutdown when the server is stopping.
///     </para>
/// </remarks>
public interface IHeartbeatManager : IDisposable
{
    /// <summary>
    ///     Starts the heartbeat timer, periodically sending heartbeats with the current connection count.
    /// </summary>
    /// <param name="connectionCountProvider">A function that returns the current connection count.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the asynchronous start operation.</returns>
    Task StartAsync(
        Func<int> connectionCountProvider,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Stops the heartbeat timer and unregisters the server from the directory.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the asynchronous stop operation.</returns>
    Task StopAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Gets the unique identifier for this server instance.
    /// </summary>
    string ServerId { get; }
}
