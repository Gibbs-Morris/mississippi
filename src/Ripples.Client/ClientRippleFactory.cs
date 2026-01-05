using System.Net.Http;

using Microsoft.Extensions.Logging;

using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples.Client;

/// <summary>
///     Factory for creating <see cref="ClientRipple{TProjection}" /> instances.
/// </summary>
/// <typeparam name="TProjection">The type of projection.</typeparam>
internal sealed class ClientRippleFactory<TProjection> : IRippleFactory<TProjection>
    where TProjection : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ClientRippleFactory{TProjection}" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="signalRConnection">The SignalR connection.</param>
    /// <param name="routeProvider">The route provider.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public ClientRippleFactory(
        HttpClient httpClient,
        ISignalRRippleConnection signalRConnection,
        IProjectionRouteProvider routeProvider,
        ILoggerFactory loggerFactory
    )
    {
        HttpClient = httpClient;
        SignalRConnection = signalRConnection;
        RouteProvider = routeProvider;
        LoggerFactory = loggerFactory;
    }

    private HttpClient HttpClient { get; }

    private ILoggerFactory LoggerFactory { get; }

    private IProjectionRouteProvider RouteProvider { get; }

    private ISignalRRippleConnection SignalRConnection { get; }

    /// <inheritdoc />
    public IRipple<TProjection> Create()
    {
        ILogger<ClientRipple<TProjection>> logger = LoggerFactory.CreateLogger<ClientRipple<TProjection>>();
        return new ClientRipple<TProjection>(HttpClient, SignalRConnection, RouteProvider, logger);
    }
}