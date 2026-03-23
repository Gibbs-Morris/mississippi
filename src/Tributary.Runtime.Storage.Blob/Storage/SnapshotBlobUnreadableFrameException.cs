using System;
using System.IO;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Represents a fail-fast read failure for a stored Blob frame that cannot be treated as a valid snapshot.
/// </summary>
internal sealed class SnapshotBlobUnreadableFrameException : IOException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobUnreadableFrameException" /> class.
    /// </summary>
    public SnapshotBlobUnreadableFrameException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobUnreadableFrameException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public SnapshotBlobUnreadableFrameException(
        string message
    )
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobUnreadableFrameException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SnapshotBlobUnreadableFrameException(
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobUnreadableFrameException" /> class.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key being read.</param>
    /// <param name="reason">The unreadable-frame reason.</param>
    /// <param name="message">The detailed failure message.</param>
    /// <param name="innerException">The inner exception when present.</param>
    public SnapshotBlobUnreadableFrameException(
        SnapshotKey snapshotKey,
        SnapshotBlobUnreadableFrameReason reason,
        string message,
        Exception? innerException = null
    )
        : base(message, innerException)
    {
        SnapshotKey = snapshotKey;
        Reason = reason;
    }

    /// <summary>
    ///     Gets the unreadable-frame reason.
    /// </summary>
    public SnapshotBlobUnreadableFrameReason Reason { get; } = SnapshotBlobUnreadableFrameReason.InvalidHeader;

    /// <summary>
    ///     Gets the snapshot key being read.
    /// </summary>
    public SnapshotKey SnapshotKey { get; }
}