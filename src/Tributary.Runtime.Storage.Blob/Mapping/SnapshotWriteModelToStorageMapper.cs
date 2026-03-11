using System.Linq;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Tributary.Runtime.Storage.Blob.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Mapping;

/// <summary>
///     Maps snapshot write models to Blob storage models.
/// </summary>
internal sealed class SnapshotWriteModelToStorageMapper : IMapper<SnapshotWriteModel, SnapshotBlobStorageModel>
{
    /// <inheritdoc />
    public SnapshotBlobStorageModel Map(
        SnapshotWriteModel input
    )
    {
        byte[] data = input.Snapshot.Data.ToArray();
        return new(
            input.Key.Stream,
            input.Key.Version,
            data,
            input.Snapshot.DataContentType,
            input.Snapshot.DataSizeBytes > 0 ? input.Snapshot.DataSizeBytes : data.LongLength);
    }
}
