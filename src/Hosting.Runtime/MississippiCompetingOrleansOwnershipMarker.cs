namespace Mississippi.Hosting.Runtime;

/// <summary>
///     Marks a host or silo service graph as already owning a competing top-level Orleans attachment.
/// </summary>
internal sealed class MississippiCompetingOrleansOwnershipMarker
{
    /// <summary>
    ///     Gets the shared competing-ownership marker instance.
    /// </summary>
    internal static MississippiCompetingOrleansOwnershipMarker Instance { get; } = new();
}