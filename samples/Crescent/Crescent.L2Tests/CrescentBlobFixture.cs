using Microsoft.Extensions.Hosting;

using Mississippi.Common.Abstractions;
using Mississippi.EventSourcing.Snapshots.Blob;


namespace Crescent.Crescent.L2Tests;

/// <summary>
///     xUnit fixture that starts the Crescent AppHost with Blob Storage Snapshot Storage.
/// </summary>
#pragma warning disable CA1515 // Types can be made internal - xUnit fixture must be public
public sealed class CrescentBlobFixture : CrescentFixtureBase
{
    /// <inheritdoc />
    protected override void AddSnapshotStorage(IHostApplicationBuilder builder)
    {
        builder.Services.AddBlobSnapshotStorageProvider(options =>
        {
            options.BlobServiceClientKey = MississippiDefaults.ServiceKeys.BlobSnapshotsClient;
            options.ContainerName = MississippiDefaults.ContainerIds.Snapshots;

            // Emulator supports Hot
            options.DefaultAccessTier = Azure.Storage.Blobs.Models.AccessTier.Hot;
        });
    }
}
#pragma warning restore CA1515
