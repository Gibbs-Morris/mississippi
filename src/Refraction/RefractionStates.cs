namespace Mississippi.Refraction;

/// <summary>
///     Component state values used across all Refraction components.
/// </summary>
public static class RefractionStates
{
    /// <summary>Component is acknowledged.</summary>
    public const string Acknowledged = "acknowledged";

    /// <summary>Component is active and accepting input.</summary>
    public const string Active = "active";

    /// <summary>Alert state.</summary>
    public const string Alert = "alert";

    /// <summary>Component is armed and ready.</summary>
    public const string Armed = "armed";

    /// <summary>Component is busy processing.</summary>
    public const string Busy = "busy";

    /// <summary>Action was cancelled.</summary>
    public const string Cancelled = "cancelled";

    /// <summary>Selection is committed.</summary>
    public const string Committed = "committed";

    /// <summary>Progress is complete.</summary>
    public const string Complete = "complete";

    /// <summary>Confirmed state.</summary>
    public const string Confirmed = "confirmed";

    /// <summary>Component is in critical state.</summary>
    public const string Critical = "critical";

    /// <summary>Determinate progress state.</summary>
    public const string Determinate = "determinate";

    /// <summary>Disabled state.</summary>
    public const string Disabled = "disabled";

    /// <summary>Component is dormant but present.</summary>
    public const string Dormant = "dormant";

    /// <summary>Component is in an error state.</summary>
    public const string Error = "error";

    /// <summary>Component is expanded.</summary>
    public const string Expanded = "expanded";

    /// <summary>Component has focus.</summary>
    public const string Focused = "focused";

    /// <summary>Default idle state.</summary>
    public const string Idle = "idle";

    /// <summary>Indeterminate progress state.</summary>
    public const string Indeterminate = "indeterminate";

    /// <summary>Invalid input state.</summary>
    public const string Invalid = "invalid";

    /// <summary>Component is latent (hidden until intent).</summary>
    public const string Latent = "latent";

    /// <summary>Component is locked/frozen during transition.</summary>
    public const string Locked = "locked";

    /// <summary>Component shows a new notification.</summary>
    public const string New = "new";

    /// <summary>Component is pinned open.</summary>
    public const string Pinned = "pinned";

    /// <summary>Quiet/minimal telemetry mode.</summary>
    public const string Quiet = "quiet";

    /// <summary>Read-only state.</summary>
    public const string ReadOnly = "readonly";

    /// <summary>Selection is being made.</summary>
    public const string Selecting = "selecting";

    /// <summary>Stacked notifications.</summary>
    public const string Stacked = "stacked";

    /// <summary>Component is tracking movement.</summary>
    public const string Tracking = "tracking";
}