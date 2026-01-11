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

    private IDisposable? MessageSubscription { get; set; }

    private IDisposable? GreetingSubscription { get; set; }

    private IDisposable? StreamMessageSubscription { get; set; }

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

        MessageSubscription = HubConnection.On<string>(
            "ReceiveMessage",
            message => MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message)));

        // Subscribe to greeting broadcasts from the GreeterGrain via SignalR
        GreetingSubscription = HubConnection.On<string, string, DateTimeOffset>(
            "ReceiveGreeting",
            (greeting, uppercaseName, generatedAt) =>
                GreetingReceived?.Invoke(this, new GreetingReceivedEventArgs(greeting, uppercaseName, generatedAt)));

        // Subscribe to Orleans stream messages bridged via SignalR
        StreamMessageSubscription = HubConnection.On<string, string, DateTimeOffset>(
            "ReceiveStreamMessage",
            (content, sender, timestamp) =>
                StreamMessageReceived?.Invoke(this, new StreamMessageReceivedEventArgs(content, sender, timestamp)));
    }

    /// <inheritdoc />
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <inheritdoc />
    public event EventHandler<GreetingReceivedEventArgs>? GreetingReceived;

    /// <inheritdoc />
    public event EventHandler<StreamMessageReceivedEventArgs>? StreamMessageReceived;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        MessageSubscription?.Dispose();
        GreetingSubscription?.Dispose();
        StreamMessageSubscription?.Dispose();
        await HubConnection.DisposeAsync();
    }

    /// <inheritdoc />
    public Task SendMessageAsync(
        string message
    ) =>
        HubConnection.SendAsync("SendMessageAsync", message);

    /// <inheritdoc />
    public Task GreetAsync(
        string name
    ) =>
        HubConnection.SendAsync("GreetAsync", name);

    /// <inheritdoc />
    public Task<string> ToUpperAsync(
        string input
    ) =>
        HubConnection.InvokeAsync<string>("ToUpperAsync", input);

    /// <inheritdoc />
    public Task StartAsync() =>
        HubConnection.StartAsync();

    /// <inheritdoc />
    public Task StopAsync() =>
        HubConnection.StopAsync();
}
