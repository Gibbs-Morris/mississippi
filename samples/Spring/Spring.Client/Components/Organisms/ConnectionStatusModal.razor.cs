using Microsoft.AspNetCore.Components;


namespace Spring.Client.Components.Organisms;

/// <summary>
///     Connection status modal.
/// </summary>
public sealed partial class ConnectionStatusModal
{
    /// <summary>Gets or sets the connection id display text.</summary>
    [Parameter]
    public string ConnectionIdText { get; set; } = "—";

    /// <summary>Gets or sets the connection status display text.</summary>
    [Parameter]
    public string ConnectionStatusText { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the modal is open.</summary>
    [Parameter]
    public bool IsOpen { get; set; }

    /// <summary>Gets or sets the last connected timestamp display text.</summary>
    [Parameter]
    public string LastConnectedAtText { get; set; } = "—";

    /// <summary>Gets or sets the last disconnected timestamp display text.</summary>
    [Parameter]
    public string LastDisconnectedAtText { get; set; } = "—";

    /// <summary>Gets or sets the last error message.</summary>
    [Parameter]
    public string? LastError { get; set; }

    /// <summary>Gets or sets the last message timestamp display text.</summary>
    [Parameter]
    public string LastMessageReceivedAtText { get; set; } = "—";

    /// <summary>Gets or sets the close callback.</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>Gets or sets the reconnect attempt count.</summary>
    [Parameter]
    public int ReconnectAttemptCount { get; set; }
}