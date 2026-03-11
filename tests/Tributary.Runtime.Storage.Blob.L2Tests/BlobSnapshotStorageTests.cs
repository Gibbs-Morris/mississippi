using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs.Models;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L2Tests;

/// <summary>
///     End-to-end tests for the Blob snapshot storage provider against Azurite.
/// </summary>
[Collection(BlobSnapshotStorageCollection.Name)]
#pragma warning disable CA1515
public sealed class BlobSnapshotStorageTests
#pragma warning restore CA1515
{
    private readonly BlobSnapshotStorageFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobSnapshotStorageTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Azurite fixture.</param>
    public BlobSnapshotStorageTests(
        BlobSnapshotStorageFixture fixture
    ) =>
        this.fixture = fixture;

    /// <summary>
    ///     Ensures host startup creates the configured Blob container.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AzuriteBlobFact]
    public async Task HostedInitializationShouldCreateContainer()
    {
        fixture.EnsureAzuriteAvailable();
        string containerName = $"snapshot-init-{Guid.NewGuid():N}";
        using IHost host = await fixture.CreateProviderHostAsync(containerName, compressionEnabled: false);
        try
        {
            bool exists = await fixture.CreateBlobServiceClient().GetBlobContainerClient(containerName).ExistsAsync();
            exists.Should().BeTrue();
        }
        finally
        {
            await host.StopAsync();
        }
    }

    /// <summary>
    ///     Ensures write, read, and delete operations round-trip snapshot content.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AzuriteBlobFact]
    public async Task WriteReadDeleteShouldRoundTripSnapshot()
    {
        fixture.EnsureAzuriteAvailable();
        string containerName = $"snapshot-roundtrip-{Guid.NewGuid():N}";
        using IHost host = await fixture.CreateProviderHostAsync(containerName, compressionEnabled: false);
        try
        {
            ISnapshotStorageProvider provider = host.Services.GetRequiredService<ISnapshotStorageProvider>();
            SnapshotKey snapshotKey = CreateSnapshotKey(Guid.NewGuid().ToString("N"), 7);
            SnapshotEnvelope expected = CreateEnvelope("roundtrip");
            await provider.WriteAsync(snapshotKey, expected, CancellationToken.None);
            SnapshotEnvelope? actual = await provider.ReadAsync(snapshotKey, CancellationToken.None);
            actual.Should().NotBeNull();
            actual!.Data.ToArray().Should().Equal(expected.Data.ToArray());
            actual.DataContentType.Should().Be(expected.DataContentType);
            actual.ReducerHash.Should().Be(expected.ReducerHash);
            await provider.DeleteAsync(snapshotKey, CancellationToken.None);
            SnapshotEnvelope? deleted = await provider.ReadAsync(snapshotKey, CancellationToken.None);
            deleted.Should().BeNull();
        }
        finally
        {
            await host.StopAsync();
        }
    }

    /// <summary>
    ///     Ensures DeleteAllAsync removes every version for a stream.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AzuriteBlobFact]
    public async Task DeleteAllShouldRemoveAllVersions()
    {
        fixture.EnsureAzuriteAvailable();
        string containerName = $"snapshot-deleteall-{Guid.NewGuid():N}";
        using IHost host = await fixture.CreateProviderHostAsync(containerName, compressionEnabled: false);
        try
        {
            ISnapshotStorageProvider provider = host.Services.GetRequiredService<ISnapshotStorageProvider>();
            SnapshotStreamKey streamKey = CreateStreamKey(Guid.NewGuid().ToString("N"));
            await provider.WriteAsync(new(streamKey, 1), CreateEnvelope("v1"), CancellationToken.None);
            await provider.WriteAsync(new(streamKey, 2), CreateEnvelope("v2"), CancellationToken.None);
            await provider.DeleteAllAsync(streamKey, CancellationToken.None);
            (await provider.ReadAsync(new(streamKey, 1), CancellationToken.None)).Should().BeNull();
            (await provider.ReadAsync(new(streamKey, 2), CancellationToken.None)).Should().BeNull();
        }
        finally
        {
            await host.StopAsync();
        }
    }

    /// <summary>
    ///     Ensures prune keeps modulus matches and the latest snapshot.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AzuriteBlobFact]
    public async Task PruneShouldRetainMatchingModuliAndLatestVersion()
    {
        fixture.EnsureAzuriteAvailable();
        string containerName = $"snapshot-prune-{Guid.NewGuid():N}";
        using IHost host = await fixture.CreateProviderHostAsync(containerName, compressionEnabled: false);
        try
        {
            ISnapshotStorageProvider provider = host.Services.GetRequiredService<ISnapshotStorageProvider>();
            SnapshotStreamKey streamKey = CreateStreamKey(Guid.NewGuid().ToString("N"));
            for (long version = 1; version <= 5; version++)
            {
                await provider.WriteAsync(new(streamKey, version), CreateEnvelope($"v{version}"), CancellationToken.None);
            }

            await provider.PruneAsync(streamKey, [2], CancellationToken.None);
            (await provider.ReadAsync(new(streamKey, 1), CancellationToken.None)).Should().BeNull();
            (await provider.ReadAsync(new(streamKey, 2), CancellationToken.None)).Should().NotBeNull();
            (await provider.ReadAsync(new(streamKey, 3), CancellationToken.None)).Should().BeNull();
            (await provider.ReadAsync(new(streamKey, 4), CancellationToken.None)).Should().NotBeNull();
            (await provider.ReadAsync(new(streamKey, 5), CancellationToken.None)).Should().NotBeNull();
        }
        finally
        {
            await host.StopAsync();
        }
    }

    /// <summary>
    ///     Ensures gzip compression can be enabled while preserving data fidelity.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [AzuriteBlobFact]
    public async Task CompressionEnabledShouldPersistGzipEncodedBlob()
    {
        fixture.EnsureAzuriteAvailable();
        string containerName = $"snapshot-compression-{Guid.NewGuid():N}";
        using IHost host = await fixture.CreateProviderHostAsync(containerName, compressionEnabled: true);
        try
        {
            ISnapshotStorageProvider provider = host.Services.GetRequiredService<ISnapshotStorageProvider>();
            SnapshotKey snapshotKey = CreateSnapshotKey(Guid.NewGuid().ToString("N"), 3);
            SnapshotEnvelope expected = CreateEnvelope(new string('x', 512));
            await provider.WriteAsync(snapshotKey, expected, CancellationToken.None);
            BlobDownloadResult download = await fixture.CreateBlobServiceClient()
                .GetBlobContainerClient(containerName)
                .GetBlobClient(SnapshotBlobPathBuilder.BuildBlobName(snapshotKey))
                .DownloadContentAsync();
            download.Details.ContentEncoding.Should().Be("gzip");
            SnapshotEnvelope? actual = await provider.ReadAsync(snapshotKey, CancellationToken.None);
            actual.Should().NotBeNull();
            actual!.Data.ToArray().Should().Equal(expected.Data.ToArray());
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static SnapshotEnvelope CreateEnvelope(
        string content
    )
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return new()
        {
            Data = ImmutableArray.Create(bytes),
            DataContentType = "application/json",
            DataSizeBytes = bytes.Length,
            ReducerHash = "reducers-hash",
        };
    }

    private static SnapshotKey CreateSnapshotKey(
        string entityId,
        long version
    ) =>
        new(CreateStreamKey(entityId), version);

    private static SnapshotStreamKey CreateStreamKey(
        string entityId
    ) =>
        new("TEST.BROOK", "snapshot-type", entityId, "reducers-hash");
}
