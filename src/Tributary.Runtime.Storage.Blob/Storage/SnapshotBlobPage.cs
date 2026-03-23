using System;
using System.Collections.Generic;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Represents one page of listed Blob names for a stream-local prefix.
/// </summary>
internal sealed class SnapshotBlobPage
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobPage" /> class.
    /// </summary>
    /// <param name="blobNames">The Blob names included in the page.</param>
    public SnapshotBlobPage(
        IReadOnlyList<string> blobNames
    ) =>
        BlobNames = blobNames ?? throw new ArgumentNullException(nameof(blobNames));

    /// <summary>
    ///     Gets the Blob names in the page.
    /// </summary>
    public IReadOnlyList<string> BlobNames { get; }
}