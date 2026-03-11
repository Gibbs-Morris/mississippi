using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L2Tests;

/// <summary>
///     Opt-in live Azure Blob smoke tests for the snapshot storage provider.
/// </summary>
#pragma warning disable CA1515
public sealed class LiveAzureBlobSnapshotStorageTests
#pragma warning restore CA1515
{
    /// <summary>
    ///     Ensures the Blob snapshot provider can round-trip a snapshot against a live Azure Blob account.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [LiveAzureBlobFact]
    public async Task WriteReadDeleteShouldRoundTripAgainstLiveAzure()
    {
        LiveAzureBlobTestConfiguration.TryCreate(out LiveAzureBlobTestConfiguration? configuration).Should().BeTrue();
        using IHost host = await CreateProviderHostAsync(configuration!);
        try
        {
            ISnapshotStorageProvider provider = host.Services.GetRequiredService<ISnapshotStorageProvider>();
            SnapshotKey snapshotKey = new(
                new("LIVE.BROOK", "snapshot-type", Guid.NewGuid().ToString("N"), "reducers-hash"),
                1);
            SnapshotEnvelope expected = CreateEnvelope("live");
            await provider.WriteAsync(snapshotKey, expected, CancellationToken.None);
            SnapshotEnvelope? actual = await provider.ReadAsync(snapshotKey, CancellationToken.None);
            actual.Should().NotBeNull();
            actual!.Data.ToArray().Should().Equal(expected.Data.ToArray());
            await provider.DeleteAsync(snapshotKey, CancellationToken.None);
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

    private static async Task<IHost> CreateProviderHostAsync(
        LiveAzureBlobTestConfiguration configuration
    )
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddLogging();
        builder.Services.AddKeyedSingleton(
            SnapshotBlobDefaults.BlobServiceClientServiceKey,
            (
                _,
                _
            ) => new BlobServiceClient(configuration.ConnectionString));
        builder.Services.AddBlobSnapshotStorageProvider(options =>
        {
            options.BlobServiceClientServiceKey = SnapshotBlobDefaults.BlobServiceClientServiceKey;
            options.ContainerName = configuration.ContainerName;
        });
        IHost host = builder.Build();
        await host.StartAsync();
        return host;
    }
}
