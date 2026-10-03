using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.SignalRConnection;


namespace Mississippi.Inlet.Client.ActionEffects;

/// <summary>
///     Default implementation of <see cref="IHubConnectionProvider" /> that creates
///     a real SignalR hub connection and dispatches connection state actions directly.
/// </summary>
internal sealed class HubConnectionProvider : IHubConnectionProvider
{
    private readonly Lock connectionLock = new();

    private readonly Lazy<IInletStore> lazyStore;

    private Task? connectionStartTask;

    private bool disposed;

    private int reconnectAttemptCount;

    private TaskCompletionSource? reconnectionCompletion;

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
        InletSignalRActionEffectOptions? options = null,
        TimeProvider? timeProvider = null
    )
    {
        ArgumentNullException.ThrowIfNull(navigationManager);
        ArgumentNullException.ThrowIfNull(lazyStore);
        this.lazyStore = lazyStore;
        TimeProvider = timeProvider ?? TimeProvider.System;
        InletSignalRActionEffectOptions effectOptions = options ?? new InletSignalRActionEffectOptions();
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

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        lock (connectionLock)
        {
            disposed = true;
            reconnectionCompletion?.TrySetCanceled();
        }

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
        Task? starting;
        TaskCompletionSource<Task>? starter = null;
        lock (connectionLock)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            HubConnectionState state = Connection.State;
            if (state == HubConnectionState.Connected)
            {
                return;
            }

            if ((state == HubConnectionState.Disconnected) &&
                (connectionStartTask is null || connectionStartTask.IsCompleted))
            {
                starter = new(TaskCreationOptions.RunContinuationsAsynchronously);

                // Publish the shared start before dispatching actions that can reenter readiness.
                connectionStartTask = starter.Task.Unwrap();
            }

            starting = state is HubConnectionState.Disconnected or HubConnectionState.Connecting
                ? connectionStartTask
                : null;
        }

        if (starter is not null)
        {
            starter.SetResult(StartConnectionAsync(cancellationToken));
        }

        if (starting is not null)
        {
            await starting.WaitAsync(cancellationToken);
        }

        while (!IsConnected)
        {
            if (Connection.State != HubConnectionState.Reconnecting)
            {
                throw new InvalidOperationException("The hub connection has not reached the connected state.");
            }

            await WaitForReconnectionAsync(cancellationToken);
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
        lock (connectionLock)
        {
            if (Connection.State == HubConnectionState.Disconnected)
            {
                if (exception is null)
                {
                    reconnectionCompletion?.TrySetCanceled();
                }
                else
                {
                    reconnectionCompletion?.TrySetException(exception);
                }
            }
        }

        reconnectAttemptCount = 0;
        Store.Dispatch(new SignalRDisconnectedAction(exception?.Message, TimeProvider.GetUtcNow()));
        return Task.CompletedTask;
    }

    private Task OnReconnectedAsync(
        string? connectionId
    )
    {
        lock (connectionLock)
        {
            if (IsConnected)
            {
                reconnectionCompletion?.TrySetResult();
            }
        }

        reconnectAttemptCount = 0;
        Store.Dispatch(new SignalRReconnectedAction(connectionId, TimeProvider.GetUtcNow()));
        return Task.CompletedTask;
    }

    private Task OnReconnectingAsync(
        Exception? exception
    )
    {
        reconnectAttemptCount++;
        Store.Dispatch(new SignalRReconnectingAction(exception?.Message, reconnectAttemptCount));
        return Task.CompletedTask;
    }

    private async Task StartConnectionAsync(
        CancellationToken cancellationToken
    )
    {
        Store.Dispatch(new SignalRConnectingAction());
        await Connection.StartAsync(cancellationToken);
        Store.Dispatch(new SignalRConnectedAction(Connection.ConnectionId, TimeProvider.GetUtcNow()));
    }

    private async Task WaitForReconnectionAsync(
        CancellationToken cancellationToken
    )
    {
        Task reconnecting;
        lock (connectionLock)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (IsConnected)
            {
                return;
            }

            if (Connection.State != HubConnectionState.Reconnecting)
            {
                throw new OperationCanceledException("The hub connection stopped reconnecting.");
            }

            if (reconnectionCompletion is null || reconnectionCompletion.Task.IsCompleted)
            {
                reconnectionCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            reconnecting = reconnectionCompletion.Task;
        }

        await reconnecting.WaitAsync(cancellationToken);
    }
}
