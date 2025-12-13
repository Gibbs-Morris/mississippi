using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mississippi.AspNetCore.Orleans.Authentication.Grains;
using Mississippi.AspNetCore.Orleans.Authentication.Options;
using Orleans;

namespace Mississippi.AspNetCore.Orleans.Authentication;

/// <summary>
/// Orleans-backed implementation of <see cref="ITicketStore"/> for authentication ticket storage.
/// </summary>
public sealed class OrleansTicketStore : ITicketStore
{
    private ILogger<OrleansTicketStore> Logger { get; }
    private IClusterClient ClusterClient { get; }
    private IOptions<TicketStoreOptions> Options { get; }
    private TicketSerializer TicketSerializer { get; }
    private TimeProvider TimeProvider { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrleansTicketStore"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="clusterClient">The Orleans cluster client.</param>
    /// <param name="options">The ticket store options.</param>
    /// <param name="ticketSerializer">The ticket serializer.</param>
    /// <param name="timeProvider">The time provider.</param>
    public OrleansTicketStore(
        ILogger<OrleansTicketStore> logger,
        IClusterClient clusterClient,
        IOptions<TicketStoreOptions> options,
        TicketSerializer ticketSerializer,
        TimeProvider timeProvider)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ClusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        TicketSerializer = ticketSerializer ?? throw new ArgumentNullException(nameof(ticketSerializer));
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc/>
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        if (ticket is null)
        {
            throw new ArgumentNullException(nameof(ticket));
        }

        string key = GenerateKey();
        byte[] ticketBytes = TicketSerializer.Serialize(ticket);

        DateTimeOffset expiresAt = ticket.Properties.ExpiresUtc ?? TimeProvider.GetUtcNow().Add(Options.Value.DefaultExpiration);

        AuthTicketData data = new()
        {
            TicketBytes = ticketBytes,
            ExpiresAt = expiresAt,
            LastRenewedAt = null
        };

        IAuthTicketGrain grain = ClusterClient.GetGrain<IAuthTicketGrain>(key);
        await grain.StoreAsync(data);

        OrleansTicketStoreLoggerExtensions.TicketStored(Logger, key);
        return key;
    }

    /// <inheritdoc/>
    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (ticket is null)
        {
            throw new ArgumentNullException(nameof(ticket));
        }

        byte[] ticketBytes = TicketSerializer.Serialize(ticket);
        DateTimeOffset expiresAt = ticket.Properties.ExpiresUtc ?? TimeProvider.GetUtcNow().Add(Options.Value.DefaultExpiration);

        AuthTicketData data = new()
        {
            TicketBytes = ticketBytes,
            ExpiresAt = expiresAt,
            LastRenewedAt = TimeProvider.GetUtcNow()
        };

        IAuthTicketGrain grain = ClusterClient.GetGrain<IAuthTicketGrain>(key);
        await grain.StoreAsync(data);

        OrleansTicketStoreLoggerExtensions.TicketRenewed(Logger, key);
    }

    /// <inheritdoc/>
    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        IAuthTicketGrain grain = ClusterClient.GetGrain<IAuthTicketGrain>(key);
        AuthTicketData? data = await grain.GetAsync();

        if (data is null)
        {
            OrleansTicketStoreLoggerExtensions.TicketNotFound(Logger, key);
            return null;
        }

        AuthenticationTicket? ticket = TicketSerializer.Deserialize(data.TicketBytes);
        OrleansTicketStoreLoggerExtensions.TicketRetrieved(Logger, key);
        return ticket;
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        IAuthTicketGrain grain = ClusterClient.GetGrain<IAuthTicketGrain>(key);
        await grain.RemoveAsync();

        OrleansTicketStoreLoggerExtensions.TicketRemoved(Logger, key);
    }

    private string GenerateKey()
    {
        return $"{Options.Value.KeyPrefix}{Guid.NewGuid():N}";
    }
}

/// <summary>
/// High-performance logger extensions for ticket store operations.
/// </summary>
internal static class OrleansTicketStoreLoggerExtensions
{
    private static readonly Action<ILogger, string, Exception?> TicketStoredMessage =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(1, nameof(TicketStored)),
            "Ticket stored: Key={Key}");

    private static readonly Action<ILogger, string, Exception?> TicketRenewedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(2, nameof(TicketRenewed)),
            "Ticket renewed: Key={Key}");

    private static readonly Action<ILogger, string, Exception?> TicketRetrievedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(3, nameof(TicketRetrieved)),
            "Ticket retrieved: Key={Key}");

    private static readonly Action<ILogger, string, Exception?> TicketNotFoundMessage =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(4, nameof(TicketNotFound)),
            "Ticket not found: Key={Key}");

    private static readonly Action<ILogger, string, Exception?> TicketRemovedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(5, nameof(TicketRemoved)),
            "Ticket removed: Key={Key}");

    public static void TicketStored(this ILogger<OrleansTicketStore> logger, string key) =>
        TicketStoredMessage(logger, key, null);

    public static void TicketRenewed(this ILogger<OrleansTicketStore> logger, string key) =>
        TicketRenewedMessage(logger, key, null);

    public static void TicketRetrieved(this ILogger<OrleansTicketStore> logger, string key) =>
        TicketRetrievedMessage(logger, key, null);

    public static void TicketNotFound(this ILogger<OrleansTicketStore> logger, string key) =>
        TicketNotFoundMessage(logger, key, null);

    public static void TicketRemoved(this ILogger<OrleansTicketStore> logger, string key) =>
        TicketRemovedMessage(logger, key, null);
}
