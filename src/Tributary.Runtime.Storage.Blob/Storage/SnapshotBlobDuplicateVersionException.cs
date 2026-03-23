using System;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Represents a duplicate-version write conflict for a snapshot Blob.
/// </summary>
internal sealed class SnapshotBlobDuplicateVersionException : InvalidOperationException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobDuplicateVersionException" /> class.
    /// </summary>
    public SnapshotBlobDuplicateVersionException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobDuplicateVersionException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public SnapshotBlobDuplicateVersionException(
        string message
    )
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobDuplicateVersionException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SnapshotBlobDuplicateVersionException(
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobDuplicateVersionException" /> class.
    /// </summary>
    /// <param name="snapshotKey">The conflicting snapshot key.</param>
    public SnapshotBlobDuplicateVersionException(
        SnapshotKey snapshotKey
    )
        : base($"A snapshot blob already exists for '{snapshotKey}'. Blob snapshot storage does not overwrite an existing version; delete the existing blob or choose a new snapshot version.") =>
        SnapshotKey = snapshotKey;

    /// <summary>
    ///     Gets the conflicting snapshot key.
    /// </summary>
    public SnapshotKey SnapshotKey { get; }
}