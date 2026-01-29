using Mississippi.Common.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Blob;

/// <summary>
///     Options controlling Azure Blob snapshot persistence.
/// </summary>
public sealed class SnapshotStorageOptions
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
    ///     Gets or sets the Blob container identifier for snapshots.
    /// </summary>
    public string ContainerId { get; set; } = MississippiDefaults.ContainerIds.Snapshots;
}