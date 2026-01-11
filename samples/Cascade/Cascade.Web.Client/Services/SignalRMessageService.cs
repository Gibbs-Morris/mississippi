using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;


namespace Cascade.Web.Client.Services;

/// <summary>
///     SignalR-based implementation of the message service.
/// </summary>
internal sealed class SignalRMessageService : IMessageService, IAsyncDisposable
{
    private HubConnection HubConnection { get; }

    private IDisposable? Subscription { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRMessageService" /> class.
    /// </summary>
    /// <param name="navigationManager">The navigation manager for resolving hub URL.</param>
    public SignalRMessageService(
        NavigationManager navigationManager
    )
    {
        HubConnection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/hubs/messages"))
            .WithAutomaticReconnect()
            .Build();

        Subscription = HubConnection.On<string>(
            "ReceiveMessage",
            message => MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message)));
    }

    /// <inheritdoc />
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Subscription?.Dispose();
        await HubConnection.DisposeAsync();
    }

    /// <inheritdoc />
    public Task SendMessageAsync(
        string message
    ) =>
        HubConnection.SendAsync("SendMessageAsync", message);

    /// <inheritdoc />
    public Task StartAsync() =>
        HubConnection.StartAsync();

    /// <inheritdoc />
    public Task StopAsync() =>
        HubConnection.StopAsync();
}
