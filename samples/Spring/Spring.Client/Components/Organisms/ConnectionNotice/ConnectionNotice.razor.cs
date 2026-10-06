using Microsoft.AspNetCore.Components;


namespace MississippiSamples.Spring.Client.Components.Organisms;

/// <summary>
///     Inline notice for an unavailable live projection connection.
/// </summary>
public sealed partial class ConnectionNotice
{
    /// <summary>Gets or sets the connection status text.</summary>
    [Parameter]
    public string ConnectionStatusText { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the connection information is visible.</summary>
    [Parameter]
    public bool IsOpen { get; set; }

    /// <summary>Gets or sets the last error message.</summary>
    [Parameter]
    public string? LastError { get; set; }

    /// <summary>Gets or sets the reconnect callback.</summary>
    [Parameter]
    public EventCallback OnReconnect { get; set; }

    /// <summary>Gets or sets the reconnect attempt count.</summary>
    [Parameter]
    public int ReconnectAttemptCount { get; set; }
}