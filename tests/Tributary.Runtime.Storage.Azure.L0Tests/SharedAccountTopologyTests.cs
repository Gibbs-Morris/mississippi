using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Azure;

namespace MississippiTests.Tributary.Runtime.Storage.Azure.L0Tests
{
    /// <summary>
    ///     Proves Brooks and Tributary can safely share a keyed Azure Blob account while using disjoint containers.
    /// </summary>
    public sealed class SharedAccountTopologyTests
    {
        /// <summary>
        ///     Brooks event storage and Tributary snapshot storage can share one keyed blob client without cross-container interference.
        /// </summary>
        /// <returns>A task that represents the asynchronous test operation.</returns>
        [Fact]
        public async Task BrooksAndTributaryCanShareOneKeyedBlobClientSafely()
        {
            using AzureBlobTransportTestContext context = new();
            ServiceCollection services = new();
            _ = services.AddLogging();
            _ = services.AddKeyedSingleton(
                context.SnapshotStorageOptions.BlobServiceClientServiceKey,
                (
                    _,
                    _
                ) => context.BlobServiceClient);
            _ = services.AddAzureBrookStorageProvider(options =>
            {
                options.BlobServiceClientServiceKey = context.BrookStorageOptions.BlobServiceClientServiceKey;
                options.ContainerName = context.BrookStorageOptions.ContainerName;
                options.LockContainerName = context.BrookStorageOptions.LockContainerName;
                options.InitializationMode = BrookStorageInitializationMode.ValidateOnly;
            });
            _ = services.AddAzureSnapshotStorageProvider(options =>
            {
                options.BlobServiceClientServiceKey = context.SnapshotStorageOptions.BlobServiceClientServiceKey;
                options.ContainerName = context.SnapshotStorageOptions.ContainerName;
                options.InitializationMode = SnapshotStorageInitializationMode.ValidateOnly;
                options.ListPageSize = context.SnapshotStorageOptions.ListPageSize;
            });

            using ServiceProvider provider = services.BuildServiceProvider();
            IBrookStorageProvider brookStorageProvider = provider.GetRequiredService<IBrookStorageProvider>();
            ISnapshotStorageProvider snapshotStorageProvider = provider.GetRequiredService<ISnapshotStorageProvider>();
            BrookKey brookKey = new("orders", "123");
            BrookEvent brookEvent = new()
            {
                Id = "event-1",
                Source = "orders",
                EventType = "OrderCreated",
                DataContentType = "application/json",
                Data = [1, 2, 3],
                DataSizeBytes = 3,
                Time = new DateTimeOffset(2026, 3, 28, 0, 0, 0, TimeSpan.Zero),
            };
            SnapshotKey snapshotKey = new(new SnapshotStreamKey("ORDERS", "projection", "123", "reducers-a"), 7);
            SnapshotEnvelope snapshot = new()
            {
                Data = [9, 8, 7],
                DataContentType = "application/json",
                DataSizeBytes = 3,
                ReducerHash = "reducers-a",
            };

            BrookPosition cursor = await brookStorageProvider.AppendEventsAsync(brookKey, [brookEvent]);
            await snapshotStorageProvider.WriteAsync(snapshotKey, snapshot);
            List<BrookEvent> brookEvents = [];
            await foreach (BrookEvent item in brookStorageProvider.ReadEventsAsync(new BrookRangeKey(brookKey.BrookName, brookKey.EntityId, 0, 1)))
            {
                brookEvents.Add(item);
            }

            SnapshotEnvelope? storedSnapshot = await snapshotStorageProvider.ReadAsync(snapshotKey);

            Assert.Equal(0, cursor.Value);
            BrookEvent storedEvent = Assert.Single(brookEvents);
            Assert.Equal(brookEvent.Id, storedEvent.Id);
            Assert.NotNull(storedSnapshot);
            Assert.Equal([.. snapshot.Data], [.. storedSnapshot.Data]);
            Assert.Contains(context.Handler.Requests, request => request.Contains($"/{context.BrookStorageOptions.ContainerName}/", StringComparison.Ordinal));
            Assert.Contains(context.Handler.Requests, request => request.Contains($"/{context.BrookStorageOptions.LockContainerName}/", StringComparison.Ordinal));
            Assert.Contains(context.Handler.Requests, request => request.Contains($"/{context.SnapshotStorageOptions.ContainerName}/", StringComparison.Ordinal));
            Assert.DoesNotContain(context.Handler.Requests, request =>
                request.Contains($"/{context.BrookStorageOptions.ContainerName}/", StringComparison.Ordinal) &&
                request.Contains($"/{context.SnapshotStorageOptions.ContainerName}/", StringComparison.Ordinal));
        }
    }
}
