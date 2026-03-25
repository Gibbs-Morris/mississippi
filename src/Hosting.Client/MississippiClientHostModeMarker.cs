namespace Mississippi.Hosting.Client;

/// <summary>
///     Marks a host as already composed for the Mississippi client role.
/// </summary>
internal sealed class MississippiClientHostModeMarker
{
    private MississippiClientHostModeMarker()
    {
    }

    /// <summary>
    ///     Gets the shared marker instance used for single host attachment validation.
    /// </summary>
    internal static MississippiClientHostModeMarker Instance { get; } = new();
}