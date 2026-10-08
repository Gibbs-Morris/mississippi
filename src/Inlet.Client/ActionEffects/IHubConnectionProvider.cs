using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;


namespace Mississippi.Inlet.Client.ActionEffects;

/// <summary>
///     Provides access to a SignalR hub connection for projection subscriptions.
/// </summary>
/// <remarks>
///     <para>
///         This interface enables testability by allowing the <see cref="InletSignalRActionEffect" />
///         to use a mockable hub connection provider instead of creating the connection directly.
///     </para>
/// </remarks>
public interface IHubConnectionProvider : IAsyncDisposable
{
    /// <summary>
    ///     Gets the underlying hub connection.
    /// </summary>
    HubConnection Connection { get; }

    /// <summary>
    ///     Gets a value indicating whether the hub connection is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     Ensures the hub connection is connected and ready for hub invocations.
    /// </summary>
    /// <param name="cancellationToken">
    ///     The startup caller's token controls shared connection startup. Other callers' tokens cancel only their waits.
    /// </param>
    /// <returns>A task that completes when the connection is ready, or observes startup failure or cancellation.</returns>
    /// <remarks>
    ///     Overlapping callers share startup and observe its outcome. Canceling the startup caller can therefore cancel
    ///     joined callers. During automatic reconnection, cancellation ends only the caller's readiness wait.
    /// </remarks>
    Task EnsureConnectedAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Registers a handler for connection closed events.
    /// </summary>
    /// <param name="handler">The handler to invoke when the connection is closed.</param>
    /// <remarks>
    ///     The exception parameter will be null if the connection was closed intentionally.
    /// </remarks>
    void OnClosed(
        Func<Exception?, Task> handler
    );

    /// <summary>
    ///     Registers a handler for reconnection events.
    /// </summary>
    /// <param name="handler">The handler to invoke when reconnected.</param>
    void OnReconnected(
        Func<string?, Task> handler
    );

    /// <summary>
    ///     Registers a handler for reconnecting events.
    /// </summary>
    /// <param name="handler">The handler to invoke when reconnecting starts.</param>
    /// <remarks>
    ///     The exception parameter contains the error that caused the connection to be lost.
    /// </remarks>
    void OnReconnecting(
        Func<Exception?, Task> handler
    );

    /// <summary>
    ///     Registers a handler for hub method invocations.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument.</typeparam>
    /// <typeparam name="T2">The type of the second argument.</typeparam>
    /// <typeparam name="T3">The type of the third argument.</typeparam>
    /// <param name="methodName">The name of the hub method.</param>
    /// <param name="handler">The handler to invoke when the method is called.</param>
    /// <returns>A disposable that removes the handler when disposed.</returns>
    IDisposable RegisterHandler<T1, T2, T3>(
        string methodName,
        Func<T1, T2, T3, Task> handler
    );
}