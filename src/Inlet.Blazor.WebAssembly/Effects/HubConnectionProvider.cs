using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Blazor.WebAssembly.SignalRConnection;


namespace Mississippi.Inlet.Blazor.WebAssembly.Effects;

/// <summary>
///     Default implementation of <see cref="IHubConnectionProvider" /> that creates
///     a real SignalR hub connection and dispatches connection state actions directly.
/// </summary>
internal sealed class HubConnectionProvider : IHubConnectionProvider
{
    private readonly Lazy<IInletStore> lazyStore;

    private int reconnectAttemptCount;

    private TimeProvider TimeProvider { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HubConnectionProvider" /> class.
    /// </summary>
    /// <param name="navigationManager">The navigation manager for resolving hub URL.</param>
    /// <param name="lazyStore">Lazy reference to the store (avoids circular dependency).</param>
    /// <param name="options">Options for configuring the hub connection.</param>
    /// <param name="timeProvider">
    ///     The time provider for timestamps. If null, uses <see cref="TimeProvider.System" />.
    /// </param>
    public HubConnectionProvider(
        NavigationManager navigationManager,
        Lazy<IInletStore> lazyStore,
        InletSignalREffectOptions? options = null,
        TimeProvider? timeProvider = null
    )
    {
        ArgumentNullException.ThrowIfNull(navigationManager);
        ArgumentNullException.ThrowIfNull(lazyStore);
        this.lazyStore = lazyStore;
        TimeProvider = timeProvider ?? TimeProvider.System;

        InletSignalREffectOptions effectOptions = options ?? new InletSignalREffectOptions();
        Connection = new HubConnectionBuilder().WithUrl(navigationManager.ToAbsoluteUri(effectOptions.HubPath))
            .WithAutomaticReconnect()
            .Build();

        // Subscribe to lifecycle events and dispatch actions directly
        Connection.Closed += OnClosedAsync;
        Connection.Reconnecting += OnReconnectingAsync;
        Connection.Reconnected += OnReconnectedAsync;
    }

    /// <inheritdoc />
    public HubConnection Connection { get; }

    /// <inheritdoc />
    public bool IsConnected => Connection.State == HubConnectionState.Connected;

    private IInletStore Store => lazyStore.Value;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Connection.Closed -= OnClosedAsync;
        Connection.Reconnecting -= OnReconnectingAsync;
        Connection.Reconnected -= OnReconnectedAsync;
        await Connection.DisposeAsync();
    }

    /// <inheritdoc />
    public async Task EnsureConnectedAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (Connection.State == HubConnectionState.Disconnected)
        {
            Store.Dispatch(new SignalRConnectingAction());
            await Connection.StartAsync(cancellationToken);
            Store.Dispatch(new SignalRConnectedAction(
                Connection.ConnectionId,
                TimeProvider.GetUtcNow()));
        }
    }

    /// <inheritdoc />
    public void OnClosed(
        Func<Exception?, Task> handler
    )
    {
        Connection.Closed += handler;
    }

    /// <inheritdoc />
    public void OnReconnected(
        Func<string?, Task> handler
    )
    {
        Connection.Reconnected += handler;
    }

    /// <inheritdoc />
    public void OnReconnecting(
        Func<Exception?, Task> handler
    )
    {
        Connection.Reconnecting += handler;
    }

    /// <inheritdoc />
    public IDisposable RegisterHandler<T1, T2, T3>(
        string methodName,
        Func<T1, T2, T3, Task> handler
    ) =>
        Connection.On(methodName, handler);

    private Task OnClosedAsync(
        Exception? exception
    )
    {
        reconnectAttemptCount = 0;
        Store.Dispatch(new SignalRDisconnectedAction(
            exception?.Message,
            TimeProvider.GetUtcNow()));
        return Task.CompletedTask;
    }

    private Task OnReconnectedAsync(
        string? connectionId
    )
    {
        reconnectAttemptCount = 0;
        Store.Dispatch(new SignalRReconnectedAction(
            connectionId,
            TimeProvider.GetUtcNow()));
        return Task.CompletedTask;
    }

    private Task OnReconnectingAsync(
        Exception? exception
    )
    {
        reconnectAttemptCount++;
        Store.Dispatch(new SignalRReconnectingAction(
            exception?.Message,
            reconnectAttemptCount));
        return Task.CompletedTask;
    }
}