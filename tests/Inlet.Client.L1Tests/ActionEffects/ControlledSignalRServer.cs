using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace MississippiTests.Inlet.Client.L1Tests.ActionEffects;

/// <summary>
///     Hosts a local SignalR endpoint with deterministic negotiation barriers.
/// </summary>
internal sealed class ControlledSignalRServer : IAsyncDisposable
{
    private int negotiationCount;

    private ControlledSignalRServer(
        WebApplicationBuilder builder,
        Channel<PendingNegotiation> negotiations,
        Channel<HubCallerContext> connections
    )
    {
        Application = builder.Build();
        Negotiations = negotiations;
        Connections = connections;
    }

    /// <summary>
    ///     Gets the dynamically allocated loopback address.
    /// </summary>
    internal string Address => Application.Urls.Single();

    /// <summary>
    ///     Gets the number of real negotiation requests received.
    /// </summary>
    internal int NegotiationCount => Volatile.Read(ref negotiationCount);

    private WebApplication Application { get; }

    private Channel<HubCallerContext> Connections { get; }

    private Channel<PendingNegotiation> Negotiations { get; }

    /// <summary>
    ///     Starts an isolated local SignalR endpoint.
    /// </summary>
    /// <param name="cancellationToken">The test watchdog token.</param>
    /// <returns>The running server owned by the caller.</returns>
    internal static async Task<ControlledSignalRServer> StartAsync(
        CancellationToken cancellationToken
    )
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Logging.ClearProviders();
        Channel<HubCallerContext> connections = Channel.CreateUnbounded<HubCallerContext>();
        builder.Services.AddSingleton(connections);
        builder.Services.AddSignalR();
        ControlledSignalRServer server = new(builder, Channel.CreateUnbounded<PendingNegotiation>(), connections);
        server.Application.Use(async (context, next) =>
        {
            if (context.Request.Path == "/hub/negotiate")
            {
                Interlocked.Increment(ref server.negotiationCount);
                PendingNegotiation negotiation = new();
                await server.Negotiations.Writer.WriteAsync(negotiation, context.RequestAborted);
                if (!await negotiation.WaitAsync(context.RequestAborted))
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    return;
                }
            }

            await next(context);
        });
        server.Application.MapHub<ReadinessHub>("/hub");
        try
        {
            await server.Application.StartAsync(cancellationToken);
            return server;
        }
        catch
        {
            await server.DisposeAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await Application.DisposeAsync();

    /// <summary>
    ///     Waits for the next actual server connection.
    /// </summary>
    /// <param name="cancellationToken">The test watchdog token.</param>
    /// <returns>The connected hub's context.</returns>
    internal ValueTask<HubCallerContext> NextConnectionAsync(
        CancellationToken cancellationToken
    ) =>
        Connections.Reader.ReadAsync(cancellationToken);

    /// <summary>
    ///     Waits until a negotiation request reaches its barrier.
    /// </summary>
    /// <param name="cancellationToken">The test watchdog token.</param>
    /// <returns>The request's response barrier.</returns>
    internal ValueTask<PendingNegotiation> NextNegotiationAsync(
        CancellationToken cancellationToken
    ) =>
        Negotiations.Reader.ReadAsync(cancellationToken);
}