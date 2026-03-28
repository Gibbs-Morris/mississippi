namespace Mississippi.Hosting.Gateway;

/// <summary>
///     Marks a host as already composed for Mississippi gateway mode.
/// </summary>
internal sealed class MississippiGatewayHostModeMarker
{
    /// <summary>
    ///     Gets the shared gateway host-mode marker instance.
    /// </summary>
    internal static MississippiGatewayHostModeMarker Instance { get; } = new();
}