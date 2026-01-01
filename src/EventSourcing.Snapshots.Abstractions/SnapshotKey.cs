using System;
using System.Globalization;

using Orleans;


namespace Mississippi.EventSourcing.Snapshots.Abstractions;

/// <summary>
///     Identifies a specific snapshot version for a projection, scoped by brook name, projection type, projection id, and
///     reducers hash.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Snapshots.Abstractions.SnapshotKey")]
public readonly record struct SnapshotKey
{
    private const char Separator = '|';

    private const string SeparatorString = "|";

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotKey" /> struct.
    /// </summary>
    /// <param name="stream">The snapshot stream key (brook name, projection type/id and reducers hash).</param>
    /// <param name="version">The snapshot version.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when version is negative.</exception>
    public SnapshotKey(
        SnapshotStreamKey stream,
        long version
    )
    {
        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version must be non-negative.");
        }

        Stream = stream;
        Version = version;
    }

    /// <summary>
    ///     Gets the snapshot stream key (brook name, projection type/id and reducers hash).
    /// </summary>
    [Id(0)]
    public SnapshotStreamKey Stream { get; }

    /// <summary>
    ///     Gets the snapshot version.
    /// </summary>
    [Id(1)]
    public long Version { get; }

    /// <summary>
    ///     Converts a snapshot key to its composite string representation.
    /// </summary>
    /// <param name="key">The snapshot key to convert.</param>
    /// <returns>
    ///     The string representation in the format "brookName|projectionType|projectionId|reducersHash|version".
    /// </returns>
    public static string FromSnapshotKey(
        SnapshotKey key
    ) =>
        key;

    /// <summary>
    ///     Creates a snapshot key from its composite string representation.
    /// </summary>
    /// <param name="value">The composite string value.</param>
    /// <returns>A parsed <see cref="SnapshotKey" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the value is not in the expected format.</exception>
    public static SnapshotKey FromString(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        int first = value.IndexOf(SeparatorString, StringComparison.Ordinal);
        int second = first < 0 ? -1 : value.IndexOf(SeparatorString, first + 1, StringComparison.Ordinal);
        int third = second < 0 ? -1 : value.IndexOf(SeparatorString, second + 1, StringComparison.Ordinal);
        int fourth = third < 0 ? -1 : value.IndexOf(SeparatorString, third + 1, StringComparison.Ordinal);
        int fifth = fourth < 0 ? -1 : value.IndexOf(SeparatorString, fourth + 1, StringComparison.Ordinal);
        if ((first < 0) || (second < 0) || (third < 0) || (fourth < 0) || (fifth >= 0))
        {
            throw new FormatException(
                $"Composite key must be in the form '<brookName>{Separator}<projectionType>{Separator}<projectionId>{Separator}<reducersHash>{Separator}<version>'.");
        }

        string brookName = value[..first];
        string projectionType = value[(first + 1)..second];
        string projectionId = value[(second + 1)..third];
        string reducersHash = value[(third + 1)..fourth];
        ReadOnlySpan<char> versionSpan = value.AsSpan(fourth + 1);
        if (!long.TryParse(versionSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out long version))
        {
            throw new FormatException($"Could not parse '{versionSpan}' as a {nameof(Version)} (long).");
        }

        return new(new(brookName, projectionType, projectionId, reducersHash), version);
    }

    /// <summary>
    ///     Implicitly converts a <see cref="SnapshotKey" /> to its composite string representation.
    /// </summary>
    /// <param name="key">The snapshot key to convert.</param>
    /// <returns>
    ///     The string representation in the format "brookName|projectionType|projectionId|reducersHash|version".
    /// </returns>
    public static implicit operator string(
        SnapshotKey key
    ) =>
        $"{key.Stream.BrookName}{Separator}{key.Stream.ProjectionType}{Separator}{key.Stream.ProjectionId}{Separator}{key.Stream.ReducersHash}{Separator}{key.Version}";

    /// <summary>
    ///     Implicitly converts a composite string value to a <see cref="SnapshotKey" />.
    /// </summary>
    /// <param name="value">The composite string value.</param>
    /// <returns>A parsed <see cref="SnapshotKey" />.</returns>
    public static implicit operator SnapshotKey(
        string value
    ) =>
        FromString(value);

    /// <summary>
    ///     Returns the composite string representation of this snapshot key.
    /// </summary>
    /// <returns>
    ///     The string representation in the format "brookName|projectionType|projectionId|reducersHash|version".
    /// </returns>
    public override string ToString() => this;
}