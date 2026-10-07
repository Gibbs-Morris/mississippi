using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace MississippiTests.Inlet.Client.L1Tests.ActionEffects;

/// <summary>
///     Controls the first negotiation of an isolated loopback SignalR server.
/// </summary>
internal sealed class StartupStatusServer : IAsyncDisposable
{
    private readonly TaskCompletionSource firstNegotiation = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource<int> firstResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int negotiationCount;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StartupStatusServer" /> class.
    /// </summary>
    public StartupStatusServer()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Logging.ClearProviders();
        builder.Services.AddSignalR();
        Application = builder.Build();
        Application.Use(HandleNegotiationAsync);
        Application.MapHub<StartupStatusHub>("/hubs/inlet");
    }

    /// <summary>
    ///     Gets the server's ephemeral loopback address.
    /// </summary>
    public string BaseUri => Application.Urls.Single() + "/";

    /// <summary>
    ///     Gets the barrier completed when the first negotiation arrives.
    /// </summary>
    public Task FirstNegotiation => firstNegotiation.Task;

    /// <summary>
    ///     Gets the number of negotiation requests received.
    /// </summary>
    public int NegotiationCount => Volatile.Read(ref negotiationCount);

    private WebApplication Application { get; }

    /// <summary>
    ///     Releases the first negotiation with the requested HTTP status.
    /// </summary>
    /// <param name="statusCode">The status to return, or 200 to run the real negotiation.</param>
    public void CompleteFirstNegotiation(
        int statusCode
    ) =>
        firstResponse.TrySetResult(statusCode);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        firstResponse.TrySetResult(StatusCodes.Status200OK);
        await Application.StopAsync();
        await Application.DisposeAsync();
    }

    /// <summary>
    ///     Starts the fixture-owned application.
    /// </summary>
    /// <param name="cancellationToken">The token for test cancellation.</param>
    /// <returns>A task representing application startup.</returns>
    public Task StartAsync(
        CancellationToken cancellationToken
    ) =>
        Application.StartAsync(cancellationToken);

    private async Task HandleNegotiationAsync(
        HttpContext context,
        RequestDelegate next
    )
    {
        if (context.Request.Path == "/hubs/inlet/negotiate")
        {
            int attempt = Interlocked.Increment(ref negotiationCount);
            if (attempt == 1)
            {
                firstNegotiation.TrySetResult();
                int status = await firstResponse.Task.WaitAsync(context.RequestAborted);
                if (status != StatusCodes.Status200OK)
                {
                    context.Response.StatusCode = status;
                    return;
                }
            }
        }

        await next(context);
    }
}