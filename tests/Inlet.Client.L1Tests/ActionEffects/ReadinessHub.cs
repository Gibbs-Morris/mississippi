using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;


namespace MississippiTests.Inlet.Client.L1Tests.ActionEffects;

/// <summary>
///     Provides a hub for the real SignalR dispatcher to invoke the test method.
/// </summary>
internal sealed class ReadinessHub : Hub
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReadinessHub" /> class.
    /// </summary>
    /// <param name="connections">The observed server connections.</param>
    public ReadinessHub(
        Channel<HubCallerContext> connections
    ) =>
        Connections = connections;

    private Channel<HubCallerContext> Connections { get; }

    /// <summary>
    ///     Returns a value through a real hub invocation.
    /// </summary>
    /// <param name="value">The value to return.</param>
    /// <returns>The supplied value.</returns>
    public string Echo(
        string value
    )
    {
        Context.ConnectionAborted.ThrowIfCancellationRequested();
        return value;
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        await Connections.Writer.WriteAsync(Context);
        await base.OnConnectedAsync();
    }
}