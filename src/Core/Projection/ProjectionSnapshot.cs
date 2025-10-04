namespace Mississippi.Core.Projection;

/// <summary>
///     Represents a snapshot of a projection at a specific point in time.
///     Contains the projection data along with version and path information for tracking and identification.
/// </summary>
/// <typeparam name="TModel">The type of the projection model data.</typeparam>
[GenerateSerializer]
[Alias("Mississippi.Core.ProjectionSnapshot")]
public sealed record ProjectionSnapshot<TModel>
{
    /// <summary>
    ///     Gets the actual projection model data.
    ///     This contains the computed state of the projection at the snapshot version.
    /// </summary>
    /// <value>The projection model data.</value>
    [Id(3)]
    public required TModel Data { get; init; }

    /// <summary>
    ///     Gets the version number of this projection snapshot.
    ///     Corresponds to the event sequence number up to which this projection was computed.
    /// </summary>
    /// <value>The version number as a long value.</value>
    [Id(0)]
    public long Version { get; init; }

    /// <summary>
    ///     Gets the path identifier for this projection type.
    ///     Used to distinguish between different projection types and configurations.
    /// </summary>
    /// <value>The projection path as a string.</value>
    [Id(1)]
    public required string ProjectionPath { get; init; }

    /// <summary>
    ///     Gets the path identifier for the aggregate root that this projection is based on.
    ///     Links the projection back to its source aggregate for traceability.
    /// </summary>
    /// <value>The aggregate root path as a string.</value>
    [Id(2)]
    public required string AggegrateRootPath { get; init; }
}
