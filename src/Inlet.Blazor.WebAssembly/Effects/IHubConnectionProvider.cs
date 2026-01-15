using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;


namespace Mississippi.Inlet.Blazor.WebAssembly.Effects;

/// <summary>
///     Provides access to a SignalR hub connection for projection subscriptions.
/// </summary>
/// <remarks>
///     <para>
///         This interface enables testability by allowing the <see cref="InletSignalREffect" />
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
    ///     Ensures the hub connection is started.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnsureConnectedAsync(
        CancellationToken cancellationToken = default
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

    /// <summary>
    ///     Registers a handler for reconnection events.
    /// </summary>
    /// <param name="handler">The handler to invoke when reconnected.</param>
    void OnReconnected(
        Func<string?, Task> handler
    );
}
