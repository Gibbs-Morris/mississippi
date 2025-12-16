using System;

using Orleans;


namespace Mississippi.EventSourcing.Snapshots.Abstractions;

/// <summary>
///     Identifies a logical stream of projection snapshots by projection type, projection id, and reducers hash.
///     This key is used for operations that target all versions of a projection snapshot stream.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Snapshots.Abstractions.SnapshotStreamKey")]
public readonly record struct SnapshotStreamKey
{
    private const int MaxLength = 2048;

    private const char Separator = '|';

    private const string SeparatorString = "|";

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotStreamKey" /> struct.
    /// </summary>
    /// <param name="projectionType">The projection type identifier.</param>
    /// <param name="projectionId">The projection instance identifier.</param>
    /// <param name="reducersHash">A hash representing the active reducers set for this projection.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when arguments contain the separator or exceed the maximum length.</exception>
    public SnapshotStreamKey(
        string projectionType,
        string projectionId,
        string reducersHash
    )
    {
        ValidateComponent(projectionType, nameof(projectionType));
        ValidateComponent(projectionId, nameof(projectionId));
        ValidateComponent(reducersHash, nameof(reducersHash));
        if ((projectionType.Length + projectionId.Length + reducersHash.Length + 2) > MaxLength)
        {
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");
        }

        ProjectionType = projectionType;
        ProjectionId = projectionId;
        ReducersHash = reducersHash;
    }

    /// <summary>
    ///     Gets the projection instance identifier.
    /// </summary>
    [Id(1)]
    public string ProjectionId { get; }

    /// <summary>
    ///     Gets the projection type identifier.
    /// </summary>
    [Id(0)]
    public string ProjectionType { get; }

    /// <summary>
    ///     Gets the reducers hash that scopes compatibility for this projection stream.
    /// </summary>
    [Id(2)]
    public string ReducersHash { get; }

    /// <summary>
    ///     Converts the stream key to its composite string representation.
    /// </summary>
    /// <param name="key">The stream key to convert.</param>
    /// <returns>The string representation in the format "projectionType|projectionId|reducersHash".</returns>
    public static string FromStreamKey(
        SnapshotStreamKey key
    ) =>
        key;

    /// <summary>
    ///     Creates a stream key from its composite string representation.
    /// </summary>
    /// <param name="value">The composite string value.</param>
    /// <returns>A parsed <see cref="SnapshotStreamKey" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the value is not in the expected format.</exception>
    public static SnapshotStreamKey FromString(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        int first = value.IndexOf(SeparatorString, StringComparison.Ordinal);
        int second = first < 0 ? -1 : value.IndexOf(SeparatorString, first + 1, StringComparison.Ordinal);
        int third = second < 0 ? -1 : value.IndexOf(SeparatorString, second + 1, StringComparison.Ordinal);
        if ((first < 0) || (second < 0) || (third >= 0))
        {
            throw new FormatException(
                $"Composite key must be in the form '<projectionType>{Separator}<projectionId>{Separator}<reducersHash>'.");
        }

        string projectionType = value[..first];
        string projectionId = value[(first + 1)..second];
        string reducersHash = value[(second + 1)..];
        return new(projectionType, projectionId, reducersHash);
    }

    /// <summary>
    ///     Implicitly converts a <see cref="SnapshotStreamKey" /> to its composite string representation.
    /// </summary>
    /// <param name="key">The stream key to convert.</param>
    /// <returns>The string representation in the format "projectionType|projectionId|reducersHash".</returns>
    public static implicit operator string(
        SnapshotStreamKey key
    ) =>
        $"{key.ProjectionType}{Separator}{key.ProjectionId}{Separator}{key.ReducersHash}";

    /// <summary>
    ///     Implicitly converts a composite string representation to a <see cref="SnapshotStreamKey" />.
    /// </summary>
    /// <param name="value">The composite string value.</param>
    /// <returns>A parsed <see cref="SnapshotStreamKey" />.</returns>
    public static implicit operator SnapshotStreamKey(
        string value
    ) =>
        FromString(value);

    private static void ValidateComponent(
        string value,
        string paramName
    )
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (value.Contains(SeparatorString, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Value cannot contain the separator character '{Separator}'.", paramName);
        }
    }

    /// <summary>
    ///     Returns the composite string representation of this stream key.
    /// </summary>
    /// <returns>The string representation in the format "projectionType|projectionId|reducersHash".</returns>
    public override string ToString() => this;
}