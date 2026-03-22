namespace Mississippi.Tributary.Runtime.Storage.Blob.L2Tests;

/// <summary>
///     Shared test collection for Azurite-backed Blob snapshot storage tests.
/// </summary>
#pragma warning disable CA1515
#pragma warning disable CA1711
[CollectionDefinition(Name)]
public sealed class BlobSnapshotStorageCollection : ICollectionFixture<BlobSnapshotStorageFixture>
#pragma warning restore CA1711
#pragma warning restore CA1515
{
    /// <summary>
    ///     The shared xUnit collection name for Blob snapshot storage tests.
    /// </summary>
    public const string Name = "BlobSnapshotStorage";
}
