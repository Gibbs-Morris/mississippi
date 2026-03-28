using System;

using Azure.Core.Pipeline;
using Azure.Storage;
using Azure.Storage.Blobs;

using Microsoft.Extensions.Options;

using Mississippi.Brooks.Runtime.Storage.Azure;
using Mississippi.Tributary.Runtime.Storage.Azure;

namespace MississippiTests.Tributary.Runtime.Storage.Azure.L0Tests
{
    /// <summary>
    ///     Owns a deterministic Azure Blob service client backed by the in-memory Azure test transport.
    /// </summary>
    internal sealed class AzureBlobTransportTestContext : IDisposable
    {
        private readonly StorageSharedKeyCredential credential = new(
            "testaccount",
            Convert.ToBase64String(new byte[32]));

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureBlobTransportTestContext" /> class.
        /// </summary>
        /// <param name="snapshotStorageOptions">The Tributary Azure options used by the runtime under test.</param>
        /// <param name="brookStorageOptions">The Brooks Azure options used by the runtime under test.</param>
        internal AzureBlobTransportTestContext(
            SnapshotStorageOptions? snapshotStorageOptions = null,
            BrookStorageOptions? brookStorageOptions = null
        )
        {
            SnapshotStorageOptions = snapshotStorageOptions ?? new SnapshotStorageOptions
            {
                BlobServiceClientServiceKey = "shared-account",
                ContainerName = "snapshots-prod",
                ListPageSize = 25,
            };

            BrookStorageOptions = brookStorageOptions ?? new BrookStorageOptions
            {
                BlobServiceClientServiceKey = SnapshotStorageOptions.BlobServiceClientServiceKey,
                ContainerName = "brooks-prod",
                LockContainerName = "locks-prod",
            };

            Transport = new InMemoryAzureBlobTransport();
            Handler = new AzureTestHttpMessageHandler(Transport.Handle);

            BlobClientOptions blobClientOptions = new()
            {
                Transport = new HttpClientTransport(Handler),
            };
            blobClientOptions.Retry.MaxRetries = 0;

            BlobServiceClient = new BlobServiceClient(
                new Uri("https://testaccount.blob.core.windows.net/"),
                credential,
                blobClientOptions);
        }

        /// <summary>
        ///     Gets the deterministic Azure Blob service client under test.
        /// </summary>
        internal BlobServiceClient BlobServiceClient { get; }

        /// <summary>
        ///     Gets the Brooks Azure options bound to this test context.
        /// </summary>
        internal BrookStorageOptions BrookStorageOptions { get; }

        /// <summary>
        ///     Gets the HTTP request recorder used by the fake Azure transport.
        /// </summary>
        internal AzureTestHttpMessageHandler Handler { get; }

        /// <summary>
        ///     Gets the Tributary Azure options bound to this test context.
        /// </summary>
        internal SnapshotStorageOptions SnapshotStorageOptions { get; }

        /// <summary>
        ///     Gets the in-memory Azure Blob transport state.
        /// </summary>
        internal InMemoryAzureBlobTransport Transport { get; }

        /// <summary>
        ///     Creates the <see cref="IOptions{TOptions}" /> wrapper used by Brooks Azure runtime services.
        /// </summary>
        /// <returns>The options wrapper.</returns>
        internal IOptions<BrookStorageOptions> CreateBrookOptions()
        {
            return Options.Create(BrookStorageOptions);
        }

        /// <summary>
        ///     Creates the <see cref="IOptions{TOptions}" /> wrapper used by Tributary Azure runtime services.
        /// </summary>
        /// <returns>The options wrapper.</returns>
        internal IOptions<SnapshotStorageOptions> CreateSnapshotOptions()
        {
            return Options.Create(SnapshotStorageOptions);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Handler.Dispose();
        }
    }
}
