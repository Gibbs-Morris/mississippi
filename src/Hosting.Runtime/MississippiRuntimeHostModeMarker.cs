namespace Mississippi.Hosting.Runtime;

/// <summary>
///     Marks a host as already composed for Mississippi runtime mode.
/// </summary>
internal sealed class MississippiRuntimeHostModeMarker
{
    /// <summary>
    ///     Gets the shared runtime host-mode marker instance.
    /// </summary>
    internal static MississippiRuntimeHostModeMarker Instance { get; } = new();
}