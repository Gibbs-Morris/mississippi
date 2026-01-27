using Mississippi.Inlet.Abstractions;


namespace Mississippi.Inlet.Client.L0Tests.Helpers;

/// <summary>
///     Test projection decorated with ProjectionPathAttribute.
/// </summary>
[ProjectionPath("test/decorated")]
internal sealed record DecoratedTestProjection
{
    /// <summary>
    ///     Gets the identifier.
    /// </summary>
    public string Id { get; init; } = string.Empty;
}