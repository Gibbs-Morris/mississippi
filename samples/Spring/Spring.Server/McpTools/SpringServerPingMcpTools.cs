using System;
using System.ComponentModel;

using ModelContextProtocol.Server;


namespace Spring.Server.McpTools;

/// <summary>
///     Exposes lightweight diagnostic MCP tools for Spring.Server.
/// </summary>
[McpServerToolType]
public sealed class SpringServerPingMcpTools
{
    private TimeProvider TimeProvider { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SpringServerPingMcpTools" /> class.
    /// </summary>
    /// <param name="timeProvider">The time provider for deterministic timestamps.</param>
    public SpringServerPingMcpTools(
        TimeProvider timeProvider
    ) =>
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <summary>
    ///     Confirms that Spring.Server is alive and reachable via MCP.
    /// </summary>
    /// <returns>A status string with server name and current UTC time, confirming Spring.Server is online.</returns>
    [McpServerTool(Name = "spring_server_ping", Title = "Spring Server Ping", ReadOnly = true, Idempotent = true)]
    [Description("Connectivity probe — returns a status message confirming Spring.Server is alive and reachable.")]
    public string Ping() => $"Spring.Server is online ({TimeProvider.GetUtcNow().UtcDateTime:u})";
}