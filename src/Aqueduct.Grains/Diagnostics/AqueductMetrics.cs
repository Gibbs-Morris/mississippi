using System.Diagnostics;
using System.Diagnostics.Metrics;


namespace Mississippi.Aqueduct.Grains.Diagnostics;

/// <summary>
///     OpenTelemetry metrics for Aqueduct SignalR backplane.
/// </summary>
internal static class AqueductMetrics
{
    /// <summary>
    ///     The meter name for Aqueduct metrics.
    /// </summary>
    internal const string MeterName = "Mississippi.Aqueduct";

    private static readonly Meter AqueductMeter = new(MeterName);

    // Client metrics
    private static readonly Counter<long> ClientConnect = AqueductMeter.CreateCounter<long>(
        "signalr.client.connect",
        "events",
        "Number of client connection events.");

    private static readonly Counter<long> ClientDisconnect = AqueductMeter.CreateCounter<long>(
        "signalr.client.disconnect",
        "events",
        "Number of client disconnection events.");

    private static readonly Histogram<double> ClientMessageDuration = AqueductMeter.CreateHistogram<double>(
        "signalr.client.message.duration",
        "ms",
        "Time to send message to client.");

    private static readonly Counter<long> ClientMessageSent = AqueductMeter.CreateCounter<long>(
        "signalr.client.message.sent",
        "messages",
        "Number of messages sent to clients.");

    private static readonly Histogram<int> GroupFanoutSize = AqueductMeter.CreateHistogram<int>(
        "signalr.group.fanout.size",
        "connections",
        "Number of connections per group broadcast.");

    // Group metrics
    private static readonly Counter<long> GroupJoin = AqueductMeter.CreateCounter<long>(
        "signalr.group.join",
        "events",
        "Number of group join events.");

    private static readonly Counter<long> GroupLeave = AqueductMeter.CreateCounter<long>(
        "signalr.group.leave",
        "events",
        "Number of group leave events.");

    private static readonly Counter<long> GroupMessageSent = AqueductMeter.CreateCounter<long>(
        "signalr.group.message.sent",
        "messages",
        "Number of messages sent to groups.");

    private static readonly Counter<long> ServerDead = AqueductMeter.CreateCounter<long>(
        "signalr.server.dead",
        "servers",
        "Number of dead servers detected.");

    private static readonly Counter<long> ServerHeartbeat = AqueductMeter.CreateCounter<long>(
        "signalr.server.heartbeat",
        "heartbeats",
        "Number of server heartbeats received.");

    // Server metrics
    private static readonly Counter<long> ServerRegister = AqueductMeter.CreateCounter<long>(
        "signalr.server.register",
        "events",
        "Number of server registration events.");

    /// <summary>
    ///     Record a client connection.
    /// </summary>
    /// <param name="hubName">The hub name.</param>
    internal static void RecordClientConnect(
        string hubName
    )
    {
        TagList tags = default;
        tags.Add("hub.name", hubName);
        ClientConnect.Add(1, tags);
    }

    /// <summary>
    ///     Record a client disconnection.
    /// </summary>
    /// <param name="hubName">The hub name.</param>
    internal static void RecordClientDisconnect(
        string hubName
    )
    {
        TagList tags = default;
        tags.Add("hub.name", hubName);
        ClientDisconnect.Add(1, tags);
    }

    /// <summary>
    ///     Record a message sent to a client.
    /// </summary>
    /// <param name="hubName">The hub name.</param>
    /// <param name="method">The SignalR method name.</param>
    /// <param name="durationMs">The duration of the send operation in milliseconds.</param>
    internal static void RecordClientMessageSent(
        string hubName,
        string method,
        double durationMs
    )
    {
        TagList tags = default;
        tags.Add("hub.name", hubName);
        tags.Add("method", method);
        ClientMessageSent.Add(1, tags);
        ClientMessageDuration.Record(durationMs, tags);
    }

    /// <summary>
    ///     Record dead servers detected.
    /// </summary>
    /// <param name="count">The number of dead servers.</param>
    internal static void RecordDeadServers(
        int count
    )
    {
        if (count > 0)
        {
            ServerDead.Add(count);
        }
    }

    /// <summary>
    ///     Record a group join.
    /// </summary>
    /// <param name="hubName">The hub name.</param>
    internal static void RecordGroupJoin(
        string hubName
    )
    {
        TagList tags = default;
        tags.Add("hub.name", hubName);
        GroupJoin.Add(1, tags);
    }

    /// <summary>
    ///     Record a group leave.
    /// </summary>
    /// <param name="hubName">The hub name.</param>
    internal static void RecordGroupLeave(
        string hubName
    )
    {
        TagList tags = default;
        tags.Add("hub.name", hubName);
        GroupLeave.Add(1, tags);
    }

    /// <summary>
    ///     Record a group message broadcast.
    /// </summary>
    /// <param name="hubName">The hub name.</param>
    /// <param name="method">The SignalR method name.</param>
    /// <param name="connectionCount">The number of connections in the group.</param>
    internal static void RecordGroupMessageSent(
        string hubName,
        string method,
        int connectionCount
    )
    {
        TagList tags = default;
        tags.Add("hub.name", hubName);
        tags.Add("method", method);
        GroupMessageSent.Add(1, tags);
        GroupFanoutSize.Record(connectionCount, tags);
    }

    /// <summary>
    ///     Record a server heartbeat.
    /// </summary>
    internal static void RecordServerHeartbeat()
    {
        ServerHeartbeat.Add(1);
    }

    /// <summary>
    ///     Record a server registration.
    /// </summary>
    internal static void RecordServerRegister()
    {
        ServerRegister.Add(1);
    }
}