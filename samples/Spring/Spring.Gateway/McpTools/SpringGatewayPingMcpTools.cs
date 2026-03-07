using System;
using System.ComponentModel;

using ModelContextProtocol.Server;


namespace Spring.Gateway.McpTools;

/// <summary>
///     Exposes lightweight diagnostic MCP tools for Spring.Gateway.
/// </summary>
[McpServerToolType]
public sealed class SpringGatewayPingMcpTools
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SpringGatewayPingMcpTools" /> class.
    /// </summary>
    /// <param name="timeProvider">The time provider for deterministic timestamps.</param>
    public SpringGatewayPingMcpTools(
        TimeProvider timeProvider
    ) =>
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    private TimeProvider TimeProvider { get; }

    /// <summary>
    ///     Confirms that Spring.Gateway is alive and reachable via MCP.
    /// </summary>
    /// <returns>A status string with gateway name and current UTC time, confirming Spring.Gateway is online.</returns>
    [McpServerTool(Name = "spring_gateway_ping", Title = "Spring Gateway Ping", ReadOnly = true, Idempotent = true)]
    [Description("Connectivity probe — returns a status message confirming Spring.Gateway is alive and reachable.")]
    public string Ping() => $"Spring.Gateway is online ({TimeProvider.GetUtcNow().UtcDateTime:u})";
}