namespace Mississippi.Ripples.Client;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Ripples.Abstractions;

/// <summary>
/// SignalR connection for receiving projection updates in Blazor WebAssembly.
/// </summary>
/// <remarks>
/// <para>
/// Manages the SignalR connection lifecycle with automatic reconnection
/// using exponential backoff. Subscriptions are maintained across reconnections.
/// </para>
/// </remarks>
internal sealed class SignalRRippleConnection : ISignalRRippleConnection
{
    private readonly ConcurrentDictionary<string, SubscriptionGroup> subscriptions = new();
    private readonly SemaphoreSlim connectionLock = new(1, 1);
    private readonly HubConnection hubConnection;
    private readonly IDisposable projectionUpdatedSubscription;

    private RipplesClientOptions Options { get; }

    private ILogger<SignalRRippleConnection> Logger { get; }

    private SignalRConnectionState state = SignalRConnectionState.Disconnected;
    private bool isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRRippleConnection"/> class.
    /// </summary>
    /// <param name="options">The client options.</param>
    /// <param name="logger">The logger.</param>
    public SignalRRippleConnection(
        IOptions<RipplesClientOptions> options,
        ILogger<SignalRRippleConnection> logger)
    {
        Options = options.Value;
        Logger = logger;

        string baseUrl = Options.BaseApiUri?.AbsoluteUri.TrimEnd('/') ?? throw new InvalidOperationException("BaseApiUri must be configured.");
        string hubUrl = $"{baseUrl}{Options.SignalRHubPath}";

        IHubConnectionBuilder builder = new HubConnectionBuilder()
            .WithUrl(hubUrl);

        if (Options.EnableAutoReconnect)
        {
            builder = builder.WithAutomaticReconnect(new ExponentialBackoffRetryPolicy(Options));
        }

#pragma warning disable IDISP004 // HubConnectionBuilder doesn't implement IDisposable; builder.Build() transfers ownership
        hubConnection = builder.Build();
#pragma warning restore IDISP004

        hubConnection.Closed += OnConnectionClosedAsync;
        hubConnection.Reconnecting += OnReconnectingAsync;
        hubConnection.Reconnected += OnReconnectedAsync;

        projectionUpdatedSubscription = hubConnection.On<string, string, long>(
            RippleHubConstants.ProjectionUpdatedMethod,
            OnProjectionUpdatedAsync);
    }

    /// <inheritdoc/>
    public SignalRConnectionState State => state;

    /// <inheritdoc/>
    public event EventHandler<SignalRConnectionStateChangeEventArgs>? StateChanged;

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);

        await connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (hubConnection.State == HubConnectionState.Connected)
            {
                return;
            }

            SignalRConnectionState previous = state;
            state = SignalRConnectionState.Connecting;
            RaiseStateChanged(previous, state);

            SignalRRippleConnectionLoggerExtensions.StartingSignalRConnection(Logger, Options.BaseApiUri?.AbsoluteUri ?? "(not configured)");
            await hubConnection.StartAsync(cancellationToken).ConfigureAwait(false);

            previous = state;
            state = SignalRConnectionState.Connected;
            RaiseStateChanged(previous, state);
            SignalRRippleConnectionLoggerExtensions.SignalRConnectionEstablished(Logger);

            // Re-subscribe to all active subscriptions after reconnection
            foreach ((_, SubscriptionGroup group) in subscriptions)
            {
                await InvokeSubscribeOnHubAsync(
                    group.ProjectionType,
                    group.EntityId,
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            SignalRConnectionState previous = state;
            state = SignalRConnectionState.Disconnected;
            RaiseStateChanged(previous, state, ex);
            SignalRRippleConnectionLoggerExtensions.SignalRConnectionFailed(Logger, ex);
            throw;
        }
        finally
        {
            connectionLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);

        await connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (hubConnection.State == HubConnectionState.Disconnected)
            {
                return;
            }

            SignalRRippleConnectionLoggerExtensions.StoppingSignalRConnection(Logger);
            await hubConnection.StopAsync(cancellationToken).ConfigureAwait(false);

            SignalRConnectionState previous = state;
            state = SignalRConnectionState.Disconnected;
            RaiseStateChanged(previous, state);
        }
        finally
        {
            connectionLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IDisposable> SubscribeAsync(
        string projectionType,
        string entityId,
        Action<long> callback,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentException.ThrowIfNullOrEmpty(projectionType);
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        ArgumentNullException.ThrowIfNull(callback);

        string key = CreateKey(projectionType, entityId);
        SubscriptionGroup group = subscriptions.GetOrAdd(
            key,
            _ => new SubscriptionGroup(projectionType, entityId, Logger));

        Subscription subscription = new(this, key, callback);
        group.Add(subscription);

        if (hubConnection.State == HubConnectionState.Connected)
        {
            await InvokeSubscribeOnHubAsync(projectionType, entityId, cancellationToken)
                .ConfigureAwait(false);
        }

        return subscription;
    }

    /// <inheritdoc/>
    public async Task UnsubscribeAsync(
        string projectionType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentException.ThrowIfNullOrEmpty(projectionType);
        ArgumentException.ThrowIfNullOrEmpty(entityId);

        string key = CreateKey(projectionType, entityId);
        if (subscriptions.TryRemove(key, out _) &&
            hubConnection.State == HubConnectionState.Connected)
        {
            await InvokeUnsubscribeOnHubAsync(projectionType, entityId, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        subscriptions.Clear();

        projectionUpdatedSubscription.Dispose();
        hubConnection.Closed -= OnConnectionClosedAsync;
        hubConnection.Reconnecting -= OnReconnectingAsync;
        hubConnection.Reconnected -= OnReconnectedAsync;

        await hubConnection.DisposeAsync().ConfigureAwait(false);
        connectionLock.Dispose();
    }

    private static string CreateKey(string projectionType, string entityId)
        => $"{projectionType}:{entityId}";

    private Task InvokeSubscribeOnHubAsync(
        string projectionType,
        string entityId,
        CancellationToken cancellationToken)
    {
        SignalRRippleConnectionLoggerExtensions.SubscribingToProjection(
            Logger,
            projectionType,
            entityId);
        return hubConnection.InvokeAsync(
            RippleHubConstants.SubscribeMethod,
            projectionType,
            entityId,
            cancellationToken);
    }

    private Task InvokeUnsubscribeOnHubAsync(
        string projectionType,
        string entityId,
        CancellationToken cancellationToken)
    {
        SignalRRippleConnectionLoggerExtensions.UnsubscribingFromProjection(
            Logger,
            projectionType,
            entityId);
        return hubConnection.InvokeAsync(
            RippleHubConstants.UnsubscribeMethod,
            projectionType,
            entityId,
            cancellationToken);
    }

    private Task OnProjectionUpdatedAsync(string projectionType, string entityId, long newVersion)
    {
        string key = CreateKey(projectionType, entityId);
        if (subscriptions.TryGetValue(key, out SubscriptionGroup? group))
        {
            group.NotifyAll(newVersion);
        }

        return Task.CompletedTask;
    }

    private Task OnConnectionClosedAsync(Exception? exception)
    {
        SignalRConnectionState previous = state;
        state = SignalRConnectionState.Disconnected;
        RaiseStateChanged(previous, state, exception);

        if (exception != null)
        {
            SignalRRippleConnectionLoggerExtensions.SignalRConnectionClosed(Logger, exception);
        }

        return Task.CompletedTask;
    }

    private Task OnReconnectingAsync(Exception? exception)
    {
        SignalRConnectionState previous = state;
        state = SignalRConnectionState.Reconnecting;
        RaiseStateChanged(previous, state, exception);

        SignalRRippleConnectionLoggerExtensions.SignalRReconnecting(Logger);
        return Task.CompletedTask;
    }

    private async Task OnReconnectedAsync(string? connectionId)
    {
        SignalRConnectionState previous = state;
        state = SignalRConnectionState.Connected;
        RaiseStateChanged(previous, state);

        SignalRRippleConnectionLoggerExtensions.SignalRReconnected(Logger, connectionId);

        // Re-subscribe to all active subscriptions
        foreach ((_, SubscriptionGroup group) in subscriptions)
        {
            await InvokeSubscribeOnHubAsync(
                group.ProjectionType,
                group.EntityId,
                CancellationToken.None).ConfigureAwait(false);
        }
    }

    private void RaiseStateChanged(
        SignalRConnectionState previous,
        SignalRConnectionState current,
        Exception? exception = null)
    {
        StateChanged?.Invoke(this, new SignalRConnectionStateChangeEventArgs(previous, current, exception));
    }

    private void RemoveSubscription(string key, Subscription subscription)
    {
        if (subscriptions.TryGetValue(key, out SubscriptionGroup? group))
        {
            group.Remove(subscription);
        }
    }

    private sealed class SubscriptionGroup(
        string projectionType,
        string entityId,
        ILogger<SignalRRippleConnection> logger)
    {
        private readonly ConcurrentDictionary<Subscription, byte> callbacks = new();

        public string ProjectionType { get; } = projectionType;

        public string EntityId { get; } = entityId;

        private ILogger<SignalRRippleConnection> Logger { get; } = logger;

        public void Add(Subscription subscription)
            => callbacks.TryAdd(subscription, 0);

        public void Remove(Subscription subscription)
            => callbacks.TryRemove(subscription, out _);

        public void NotifyAll(long newVersion)
        {
            foreach ((Subscription subscription, _) in callbacks)
            {
#pragma warning disable CA1031 // Do not catch general exception types
                try
                {
                    subscription.Callback(newVersion);
                }
                catch (Exception ex)
                {
                    SignalRRippleConnectionLoggerExtensions.SubscriptionCallbackFailed(
                        Logger,
                        ProjectionType,
                        EntityId,
                        ex);
                }
#pragma warning restore CA1031
            }
        }
    }

    private sealed class Subscription(
        SignalRRippleConnection connection,
        string key,
        Action<long> callback) : IDisposable
    {
        private bool isDisposed;

        public Action<long> Callback { get; } = callback;

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            connection.RemoveSubscription(key, this);
        }
    }

    private sealed class ExponentialBackoffRetryPolicy(RipplesClientOptions options) : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            if (options.MaxReconnectAttempts > 0 &&
                retryContext.PreviousRetryCount >= options.MaxReconnectAttempts)
            {
                return null;
            }

            double delayMs = options.InitialReconnectDelay.TotalMilliseconds *
                             Math.Pow(2, retryContext.PreviousRetryCount);
            double maxDelayMs = options.MaxReconnectDelay.TotalMilliseconds;

            return TimeSpan.FromMilliseconds(Math.Min(delayMs, maxDelayMs));
        }
    }
}
