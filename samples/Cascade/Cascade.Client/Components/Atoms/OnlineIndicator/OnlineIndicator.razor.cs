using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Cascade.Client.Components.Atoms.OnlineIndicator;

/// <summary>
///     Atomic component for displaying user online status.
/// </summary>
public sealed partial class OnlineIndicator : ComponentBase
{
    /// <summary>
    ///     Gets or sets any additional HTML attributes to apply to the element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the presence status. Valid values: online, away, busy, offline.
    /// </summary>
    [Parameter]
    public string Status { get; set; } = "offline";

    private string StatusClass =>
        Status switch
        {
            "online" => "online-indicator--online",
            "away" => "online-indicator--away",
            "busy" => "online-indicator--busy",
            var _ => string.Empty,
        };

    private string StatusLabel =>
        Status switch
        {
            "online" => "Online",
            "away" => "Away",
            "busy" => "Do not disturb",
            var _ => "Offline",
        };
}