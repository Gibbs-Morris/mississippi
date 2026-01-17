namespace Cascade.Contracts.Storage;

/// <summary>
///     Represents a blob item for storage operations.
/// </summary>
public sealed record BlobItem
{
    /// <summary>
    ///     Gets the blob content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    ///     Gets the blob name.
    /// </summary>
    public required string Name { get; init; }
}