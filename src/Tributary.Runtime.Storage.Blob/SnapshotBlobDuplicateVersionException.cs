using System;

namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Reports that a snapshot version already exists in Blob storage.
/// </summary>
public sealed class SnapshotBlobDuplicateVersionException : InvalidOperationException
{
    /// <summary>Initializes a new instance of the <see cref="SnapshotBlobDuplicateVersionException" /> class.</summary>
    public SnapshotBlobDuplicateVersionException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SnapshotBlobDuplicateVersionException" /> class.</summary>
    /// <param name="message">The conflict description.</param>
    public SnapshotBlobDuplicateVersionException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SnapshotBlobDuplicateVersionException" /> class.</summary>
    /// <param name="message">The conflict description.</param>
    /// <param name="innerException">The Azure Storage failure.</param>
    public SnapshotBlobDuplicateVersionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
