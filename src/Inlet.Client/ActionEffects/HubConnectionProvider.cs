using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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

    private Task stateTransition = Task.CompletedTask;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HubConnectionProvider" /> class.
    /// </summary>
    /// <param name="navigationManager">The navigation manager for resolving hub URL.</param>
    /// <param name="lazyStore">Lazy reference to the store (avoids circular dependency).</param>
    /// <param name="options">Options for configuring the hub connection.</param>
    /// <param name="timeProvider">
    ///     The time provider for timestamps. If null, uses <see cref="TimeProvider.System" />.
    /// </param>
    /// <param name="logger">The logger for connection startup diagnostics.</param>
    public HubConnectionProvider(
        NavigationManager navigationManager,
        Lazy<IInletStore> lazyStore,
        InletSignalRActionEffectOptions? options = null,
        TimeProvider? timeProvider = null,
        ILogger<HubConnectionProvider>? logger = null
    )
        : this(navigationManager, lazyStore, options, timeProvider, logger, null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HubConnectionProvider" /> class with an internal startup invocation.
    /// </summary>
    /// <param name="navigationManager">The navigation manager for resolving the hub URL.</param>
    /// <param name="lazyStore">The lazy store reference.</param>
    /// <param name="options">The hub connection options.</param>
    /// <param name="timeProvider">The timestamp provider, or the system provider when null.</param>
    /// <param name="logger">The startup logger, or the null logger when null.</param>
    /// <param name="connectionStarter">The internal startup invocation, or real SignalR startup when null.</param>
    internal HubConnectionProvider(
        NavigationManager navigationManager,
        Lazy<IInletStore> lazyStore,
        InletSignalRActionEffectOptions? options,
        TimeProvider? timeProvider,
        ILogger<HubConnectionProvider>? logger,
        Func<CancellationToken, Task>? connectionStarter
    )
    {
        ArgumentNullException.ThrowIfNull(navigationManager);
        ArgumentNullException.ThrowIfNull(lazyStore);
        this.lazyStore = lazyStore;
        TimeProvider = timeProvider ?? TimeProvider.System;
        Logger = logger ?? NullLogger<HubConnectionProvider>.Instance;
        InletSignalRActionEffectOptions effectOptions = options ?? new InletSignalRActionEffectOptions();
        Connection = new HubConnectionBuilder().WithUrl(navigationManager.ToAbsoluteUri(effectOptions.HubPath))
            .WithAutomaticReconnect()
            .Build();
        ConnectionStarter = connectionStarter ?? Connection.StartAsync;

        // Subscribe to lifecycle events and dispatch actions directly
        Connection.Closed += OnClosedAsync;
        Connection.Reconnecting += OnReconnectingAsync;
        Connection.Reconnected += OnReconnectedAsync;
    }

    /// <inheritdoc />
    public HubConnection Connection { get; }

    /// <inheritdoc />
    public bool IsConnected => Connection.State == HubConnectionState.Connected;

    private Func<CancellationToken, Task> ConnectionStarter { get; }

    private ILogger<HubConnectionProvider> Logger { get; }

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
        long started = Stopwatch.GetTimestamp();
        Logger.EnsureConnectionStarted(Connection.State);
        Task? starting;
        TaskCompletionSource<Task>? starter = null;
        lock (connectionLock)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            HubConnectionState state = Connection.State;
            if (state == HubConnectionState.Connected)
            {
                Logger.EnsureConnectionCompleted(state, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
                return;
            }

            if ((state == HubConnectionState.Disconnected) &&
                (connectionStartTask is null || connectionStartTask.IsCompleted))
            {
                starter = new(TaskCreationOptions.RunContinuationsAsynchronously);

                // Publish the shared start before dispatching actions that can reenter readiness.
                connectionStartTask = starter.Task.Unwrap();
                reconnectionCompletion?.TrySetCanceled(CancellationToken.None);
            }

            starting = state is HubConnectionState.Disconnected or HubConnectionState.Connecting
                ? connectionStartTask
                : null;
        }

        if (starter is not null)
        {
            starter.SetResult(StartConnectionAsync(started, cancellationToken));
        }

        if (starting is not null)
        {
            if (starter is not null)
            {
                // The startup owner observes failure publication before completing.
                await starting;
            }
            else
            {
                await starting.WaitAsync(cancellationToken);
            }
        }

        while (!IsConnected)
        {
            if (Connection.State != HubConnectionState.Reconnecting)
            {
                throw new InvalidOperationException("The hub connection has not reached the connected state.");
            }

            await WaitForReconnectionAsync(cancellationToken);
        }

        Logger.EnsureConnectionCompleted(Connection.State, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
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

    /// <summary>
    ///     Captures a synchronous startup failure in the returned task.
    /// </summary>
    /// <param name="cancellationToken">The caller's startup cancellation token.</param>
    /// <returns>A task representing transport startup.</returns>
    private async Task BeginConnectionAsync(
        CancellationToken cancellationToken
    ) =>
        await ConnectionStarter(cancellationToken);

    private void HandleStartupFailure(
        Exception exception,
        long started,
        CancellationToken cancellationToken
    )
    {
        Logger.ConnectionStartFailed(
            exception is OperationCanceledException && cancellationToken.IsCancellationRequested
                ? LogLevel.Information
                : LogLevel.Error,
            exception,
            Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        try
        {
            if (Connection.State == HubConnectionState.Disconnected)
            {
                Store.Dispatch(new SignalRDisconnectedAction(exception.Message, TimeProvider.GetUtcNow()));
            }
            else if (Connection.State == HubConnectionState.Connected)
            {
                Store.Dispatch(new SignalRConnectedAction(Connection.ConnectionId, TimeProvider.GetUtcNow()));
            }
        }
        catch (Exception publicationException) when (!ReferenceEquals(publicationException, exception))
        {
            Logger.ConnectionStatusPublicationFailed(
                publicationException,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }

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

    /// <summary>
    ///     Orders startup initiation and failed-status publication without holding the queue during network I/O.
    /// </summary>
    /// <typeparam name="TResult">The transition result type.</typeparam>
    /// <param name="transition">The synchronous state transition.</param>
    /// <returns>A task representing the ordered transition.</returns>
    private async Task<TResult> RunStateTransitionAsync<TResult>(
        Func<TResult> transition
    )
    {
        TaskCompletionSource completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task previous = Interlocked.Exchange(ref stateTransition, completed.Task);
        await previous;
        try
        {
            return transition();
        }
        finally
        {
            completed.SetResult();
        }
    }

    private async Task StartConnectionAsync(
        long started,
        CancellationToken cancellationToken
    )
    {
        Task starting = await RunStateTransitionAsync(() =>
        {
            Store.Dispatch(new SignalRConnectingAction());
            return BeginConnectionAsync(cancellationToken);
        });
        try
        {
            await starting;
        }
        catch (Exception exception)
        {
            await RunStateTransitionAsync(() =>
            {
                lock (connectionLock)
                {
                    // Allow an explicit retry from the failure notification without changing joined callers' outcome.
                    connectionStartTask = null;
                }

                HandleStartupFailure(exception, started, cancellationToken);
                return true;
            });
            throw;
        }

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