using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;


namespace Mississippi.Inlet.Blazor.WebAssembly.Effects;

/// <summary>
///     Default implementation of <see cref="IHubConnectionProvider" /> that creates
///     a real SignalR hub connection.
/// </summary>
internal sealed class HubConnectionProvider : IHubConnectionProvider
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="HubConnectionProvider" /> class.
    /// </summary>
    /// <param name="navigationManager">The navigation manager for resolving hub URL.</param>
    /// <param name="options">Options for configuring the hub connection.</param>
    public HubConnectionProvider(
        NavigationManager navigationManager,
        InletSignalREffectOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(navigationManager);
        InletSignalREffectOptions effectOptions = options ?? new InletSignalREffectOptions();
        Connection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri(effectOptions.HubPath))
            .WithAutomaticReconnect()
            .Build();
    }

    /// <inheritdoc />
    public HubConnection Connection { get; }

    /// <inheritdoc />
    public bool IsConnected => Connection.State == HubConnectionState.Connected;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }

    /// <inheritdoc />
    public async Task EnsureConnectedAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (Connection.State == HubConnectionState.Disconnected)
        {
            await Connection.StartAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public IDisposable RegisterHandler<T1, T2, T3>(
        string methodName,
        Func<T1, T2, T3, Task> handler
    )
    {
        return Connection.On(methodName, handler);
    }

    /// <inheritdoc />
    public void OnReconnected(
        Func<string?, Task> handler
    )
    {
        Connection.Reconnected += handler;
    }
}
