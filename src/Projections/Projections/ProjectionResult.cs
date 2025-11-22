namespace Mississippi.Projections.Projections;

/// <summary>
///     Represents an immutable result returned by a projection grain, including identifying
///     metadata and the projection model instance.
/// </summary>
/// <typeparam name="TModel">The projection model type contained in the result.</typeparam>
public sealed record ProjectionResult<TModel>
{
    /// <summary>
    ///     Gets the aggregate identifier for the projection instance.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the immutable projection model.
    /// </summary>
    public TModel Model { get; init; } = default!;

    /// <summary>
    ///     Gets the logical path that identifies the projection type or category.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the version number associated with the projection snapshot.
    /// </summary>
    public long Version { get; init; }
}