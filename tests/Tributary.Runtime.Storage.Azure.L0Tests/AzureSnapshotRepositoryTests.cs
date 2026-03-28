using System.Linq;
using System.Threading.Tasks;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Azure.Storage;

namespace MississippiTests.Tributary.Runtime.Storage.Azure.L0Tests
{
    /// <summary>
    ///     Deterministic transport-backed tests for <see cref="AzureSnapshotRepository" />.
    /// </summary>
    public sealed class AzureSnapshotRepositoryTests
    {
        private static readonly SnapshotStreamKey StreamKey = new("ORDERS", "projection", "123", "reducers-a");

        private static readonly SnapshotStreamKey OtherStreamKey = new("ORDERS", "projection", "456", "reducers-a");

        /// <summary>
        ///     Writing the same snapshot version again replaces the stored payload for that exact version.
        /// </summary>
        /// <returns>A task that represents the asynchronous test operation.</returns>
        [Fact]
        public async Task WriteAsyncOverwritesTheRequestedVersion()
        {
            using AzureBlobTransportTestContext context = new();
            AzureSnapshotRepository repository = CreateRepository(context);
            SnapshotKey snapshotKey = new(StreamKey, 4);

            await repository.WriteAsync(snapshotKey, CreateSnapshot(4));
            await repository.WriteAsync(snapshotKey, CreateSnapshot(44));

            SnapshotEnvelope? storedSnapshot = await repository.ReadAsync(snapshotKey);

            Assert.NotNull(storedSnapshot);
            Assert.Equal([44, 45], [.. storedSnapshot.Data]);
            Assert.Equal("reducers-44", storedSnapshot.ReducerHash);
        }

        /// <summary>
        ///     Snapshot reads are mapped by the exact requested version rather than returning an adjacent version.
        /// </summary>
        /// <returns>A task that represents the asynchronous test operation.</returns>
        [Fact]
        public async Task ReadAsyncReturnsTheExactRequestedVersion()
        {
            using AzureBlobTransportTestContext context = new();
            AzureSnapshotRepository repository = CreateRepository(context);
            SnapshotEnvelope versionFour = CreateSnapshot(4);
            SnapshotEnvelope versionFive = CreateSnapshot(5);

            await repository.WriteAsync(new SnapshotKey(StreamKey, 4), versionFour);
            await repository.WriteAsync(new SnapshotKey(StreamKey, 5), versionFive);

            SnapshotEnvelope? readVersionFour = await repository.ReadAsync(new SnapshotKey(StreamKey, 4));
            SnapshotEnvelope? readVersionFive = await repository.ReadAsync(new SnapshotKey(StreamKey, 5));
            SnapshotEnvelope? missingVersion = await repository.ReadAsync(new SnapshotKey(StreamKey, 6));

            Assert.NotNull(readVersionFour);
            Assert.NotNull(readVersionFive);
            Assert.Equal([.. versionFour.Data], [.. readVersionFour.Data]);
            Assert.Equal([.. versionFive.Data], [.. readVersionFive.Data]);
            Assert.Null(missingVersion);
        }

        /// <summary>
        ///     Delete-all removes only the targeted snapshot stream prefix.
        /// </summary>
        /// <returns>A task that represents the asynchronous test operation.</returns>
        [Fact]
        public async Task DeleteAllAsyncRemovesOnlyTheRequestedStreamPrefix()
        {
            using AzureBlobTransportTestContext context = new();
            AzureSnapshotRepository repository = CreateRepository(context);

            await repository.WriteAsync(new SnapshotKey(StreamKey, 1), CreateSnapshot(1));
            await repository.WriteAsync(new SnapshotKey(StreamKey, 2), CreateSnapshot(2));
            await repository.WriteAsync(new SnapshotKey(OtherStreamKey, 1), CreateSnapshot(11));

            await repository.DeleteAllAsync(StreamKey);

            Assert.Null(await repository.ReadAsync(new SnapshotKey(StreamKey, 1)));
            Assert.Null(await repository.ReadAsync(new SnapshotKey(StreamKey, 2)));
            Assert.NotNull(await repository.ReadAsync(new SnapshotKey(OtherStreamKey, 1)));
        }

        /// <summary>
        ///     Delete removes only the exact requested snapshot version.
        /// </summary>
        /// <returns>A task that represents the asynchronous test operation.</returns>
        [Fact]
        public async Task DeleteAsyncRemovesOnlyTheRequestedVersion()
        {
            using AzureBlobTransportTestContext context = new();
            AzureSnapshotRepository repository = CreateRepository(context);

            await repository.WriteAsync(new SnapshotKey(StreamKey, 4), CreateSnapshot(4));
            await repository.WriteAsync(new SnapshotKey(StreamKey, 5), CreateSnapshot(5));

            await repository.DeleteAsync(new SnapshotKey(StreamKey, 4));

            Assert.Null(await repository.ReadAsync(new SnapshotKey(StreamKey, 4)));
            Assert.NotNull(await repository.ReadAsync(new SnapshotKey(StreamKey, 5)));
        }

        /// <summary>
        ///     Pruning retains versions matching the modulus rules and always preserves the highest version.
        /// </summary>
        /// <returns>A task that represents the asynchronous test operation.</returns>
        [Fact]
        public async Task PruneAsyncRetainsMatchingModuliAndHighestVersion()
        {
            using AzureBlobTransportTestContext context = new();
            AzureSnapshotRepository repository = CreateRepository(context);

            foreach (long version in Enumerable.Range(1, 5).Select(static value => (long)value))
            {
                await repository.WriteAsync(new SnapshotKey(StreamKey, version), CreateSnapshot(version));
            }

            await repository.PruneAsync(StreamKey, [2]);

            Assert.Null(await repository.ReadAsync(new SnapshotKey(StreamKey, 1)));
            Assert.NotNull(await repository.ReadAsync(new SnapshotKey(StreamKey, 2)));
            Assert.Null(await repository.ReadAsync(new SnapshotKey(StreamKey, 3)));
            Assert.NotNull(await repository.ReadAsync(new SnapshotKey(StreamKey, 4)));
            Assert.NotNull(await repository.ReadAsync(new SnapshotKey(StreamKey, 5)));
        }

        /// <summary>
        ///     Pruning always retains the highest version even when no modulus matches it.
        /// </summary>
        /// <returns>A task that represents the asynchronous test operation.</returns>
        [Fact]
        public async Task PruneAsyncAlwaysRetainsTheHighestVersion()
        {
            using AzureBlobTransportTestContext context = new();
            AzureSnapshotRepository repository = CreateRepository(context);

            await repository.WriteAsync(new SnapshotKey(StreamKey, 3), CreateSnapshot(3));
            await repository.WriteAsync(new SnapshotKey(StreamKey, 5), CreateSnapshot(5));

            await repository.PruneAsync(StreamKey, [10]);

            Assert.Null(await repository.ReadAsync(new SnapshotKey(StreamKey, 3)));
            Assert.NotNull(await repository.ReadAsync(new SnapshotKey(StreamKey, 5)));
        }

        private static SnapshotEnvelope CreateSnapshot(
            long version
        )
        {
            return new SnapshotEnvelope
            {
                Data = [(byte)version, (byte)(version + 1)],
                DataContentType = "application/octet-stream",
                DataSizeBytes = 2,
                ReducerHash = $"reducers-{version}",
            };
        }

        private static AzureSnapshotRepository CreateRepository(
            AzureBlobTransportTestContext context
        )
        {
            return new AzureSnapshotRepository(
                context.BlobServiceClient,
                context.CreateSnapshotOptions(),
                new Sha256SnapshotPathEncoder(),
                new AzureSnapshotDocumentCodec(),
                (prefix, cancellationToken) => context.Transport.ListBlobNamesAsync(
                    context.SnapshotStorageOptions.ContainerName,
                    prefix,
                    cancellationToken));
        }
    }
}
