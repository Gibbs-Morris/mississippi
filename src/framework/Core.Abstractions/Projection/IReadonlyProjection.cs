using System.Numerics;

namespace Mississippi.Core.Abstractions.Projection;

/// <summary>
///     Defines a read-only projection interface that encapsulates a projection with version and location metadata.
/// </summary>
/// <typeparam name="T">The type of the projection data.</typeparam>
public interface IReadonlyProjection<T>
{
    /// <summary>
    ///     Gets the projection data.
    /// </summary>
    T Projection { get; init; }

    /// <summary>
    ///     Gets the version number of the projection.
    /// </summary>
    BigInteger Version { get; init; }

    /// <summary>
    ///     Gets the location metadata for the projection.
    /// </summary>
    IProjectionLocation Location { get; init; }
}