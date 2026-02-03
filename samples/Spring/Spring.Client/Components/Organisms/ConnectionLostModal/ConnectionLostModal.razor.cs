using Microsoft.AspNetCore.Components;


namespace Spring.Client.Components.Organisms;

/// <summary>
///     Connection lost modal.
/// </summary>
public sealed partial class ConnectionLostModal
{
    /// <summary>Gets or sets the connection status text.</summary>
    [Parameter]
    public string ConnectionStatusText { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the modal is open.</summary>
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