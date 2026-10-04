namespace Mississippi.Inlet.Client.L0Tests.Helpers;

/// <summary>
///     Dummy projection type for testing.
/// </summary>
internal sealed record TestProjection
{
    /// <summary>
    ///     Gets the name.
    /// </summary>
    public string Name { get; init; } = string.Empty;
}