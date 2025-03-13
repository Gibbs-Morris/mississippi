namespace Mississippi.Core.Abstractions.Projection;

/// <summary>
///     Defines the location metadata for a projection.
/// </summary>
public interface IProjectionLocation
{
    /// <summary>
    ///     Gets the path of the projection location.
    /// </summary>
    string Path { get; init; }

    /// <summary>
    ///     Gets the identifier of the projection location.
    /// </summary>
    string Id { get; init; }
}