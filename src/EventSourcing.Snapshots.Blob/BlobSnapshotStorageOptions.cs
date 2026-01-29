using Azure.Storage.Blobs.Models;

using Mississippi.Common.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Blob;

/// <summary>
///     Options controlling Azure Blob snapshot storage behavior.
/// </summary>
public sealed class BlobSnapshotStorageOptions
{
    /// <summary>
    ///     Gets or sets the keyed service key used to resolve the <c>BlobServiceClient</c> from DI.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see cref="MississippiDefaults.ServiceKeys.BlobSnapshotsClient" />.
    ///         Override this to share a single <c>BlobServiceClient</c> across multiple storage providers
    ///         or to use a custom keyed registration.
    ///     </para>
    /// </remarks>
    public string BlobServiceClientKey { get; set; } = MississippiDefaults.ServiceKeys.BlobSnapshotsClient;

    /// <summary>
    ///     Gets or sets the blob container name for snapshots.
    /// </summary>
    /// <value>Defaults to <see cref="MississippiDefaults.ContainerIds.Snapshots" />.</value>
    public string ContainerName { get; set; } = MississippiDefaults.ContainerIds.Snapshots;

    /// <summary>
    ///     Gets or sets the access tier for new snapshots.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Supported tiers: <see cref="AccessTier.Hot" />, <see cref="AccessTier.Cool" />, <see cref="AccessTier.Cold" />.
    ///         Archive tier is not supported because it requires rehydration before reads.
    ///     </para>
    ///     <para>
    ///         Note: Cool and Cold tiers have early deletion fees if deleted before their minimum retention period
    ///         (30 days for Cool, 90 days for Cold).
    ///     </para>
    /// </remarks>
    /// <value>Defaults to <see cref="AccessTier.Hot" /> for immediate access.</value>
    public AccessTier DefaultAccessTier { get; set; } = AccessTier.Hot;

    /// <summary>
    ///     Gets or sets the maximum concurrent operations for batch delete/prune.
    /// </summary>
    /// <value>Defaults to 10.</value>
    public int MaxConcurrency { get; set; } = 10;

    /// <summary>
    ///     Gets or sets the compression algorithm for writing snapshots.
    /// </summary>
    /// <value>Defaults to <see cref="SnapshotCompression.Brotli" /> for optimal compression ratio.</value>
    public SnapshotCompression WriteCompression { get; set; } = SnapshotCompression.Brotli;
}