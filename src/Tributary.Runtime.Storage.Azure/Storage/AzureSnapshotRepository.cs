using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Options;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Azure.Storage
{
    /// <summary>
    ///     Reads and writes Tributary Azure snapshot blobs.
    /// </summary>
    /// <param name="blobServiceClient">The Azure Blob service client.</param>
    /// <param name="options">The Tributary Azure storage options.</param>
    /// <param name="snapshotPathEncoder">The snapshot path encoder.</param>
    /// <param name="snapshotDocumentCodec">The snapshot document codec.</param>
    /// <param name="listBlobNamesAsync">Optional blob-name listing override used by deterministic tests.</param>
    internal sealed class AzureSnapshotRepository(
        BlobServiceClient blobServiceClient,
        IOptions<SnapshotStorageOptions> options,
        ISnapshotPathEncoder snapshotPathEncoder,
        ISnapshotDocumentCodec snapshotDocumentCodec,
        Func<string, CancellationToken, IAsyncEnumerable<string>>? listBlobNamesAsync = null) : IAzureSnapshotRepository
    {
        private BlobServiceClient BlobServiceClient { get; } = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));

        private BlobContainerClient SnapshotContainer => BlobServiceClient.GetBlobContainerClient(Options.ContainerName);

        private ISnapshotDocumentCodec SnapshotDocumentCodec { get; } = snapshotDocumentCodec ?? throw new ArgumentNullException(nameof(snapshotDocumentCodec));

        private SnapshotStorageOptions Options { get; } = options?.Value ?? throw new ArgumentNullException(nameof(options));

        private ISnapshotPathEncoder SnapshotPathEncoder { get; } = snapshotPathEncoder ?? throw new ArgumentNullException(nameof(snapshotPathEncoder));

        private Func<string, CancellationToken, IAsyncEnumerable<string>>? ListBlobNamesAsyncOverride { get; } = listBlobNamesAsync;

        /// <inheritdoc />
        public async Task DeleteAllAsync(
            SnapshotStreamKey streamKey,
            CancellationToken cancellationToken = default
        )
        {
            await foreach (string blobName in GetBlobNamesAsync(streamKey, cancellationToken).ConfigureAwait(false))
            {
                BlobClient blobClient = SnapshotContainer.GetBlobClient(blobName);
                _ = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            SnapshotKey snapshotKey,
            CancellationToken cancellationToken = default
        )
        {
            BlobClient blobClient = SnapshotContainer.GetBlobClient(SnapshotPathEncoder.GetSnapshotPath(snapshotKey));
            _ = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task PruneAsync(
            SnapshotStreamKey streamKey,
            IReadOnlyCollection<int> retainModuli,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(retainModuli);

            List<VersionedBlob> versionedBlobs = [];
            await foreach (string blobName in GetBlobNamesAsync(streamKey, cancellationToken).ConfigureAwait(false))
            {
                if (TryParseVersionedBlob(blobName, out VersionedBlob versionedBlob))
                {
                    versionedBlobs.Add(versionedBlob);
                }
            }

            if (versionedBlobs.Count == 0)
            {
                return;
            }

            long highestVersion = versionedBlobs.Max(item => item.Version);
            HashSet<long> retainedVersions =
            [..
                versionedBlobs
                    .Where(item => retainModuli.Any(modulus => (modulus != 0) && ((item.Version % modulus) == 0)))
                    .Select(item => item.Version),
                highestVersion,
            ];

            foreach (VersionedBlob versionedBlob in versionedBlobs)
            {
                if (retainedVersions.Contains(versionedBlob.Version))
                {
                    continue;
                }

                BlobClient blobClient = SnapshotContainer.GetBlobClient(versionedBlob.Name);
                _ = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<SnapshotEnvelope?> ReadAsync(
            SnapshotKey snapshotKey,
            CancellationToken cancellationToken = default
        )
        {
            BlobClient blobClient = SnapshotContainer.GetBlobClient(SnapshotPathEncoder.GetSnapshotPath(snapshotKey));

            try
            {
                Response<BlobDownloadResult> response = await blobClient.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
                return SnapshotDocumentCodec.Decode(response.Value.Content);
            }
            catch (RequestFailedException exception) when (exception.Status == 404)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public async Task WriteAsync(
            SnapshotKey snapshotKey,
            SnapshotEnvelope snapshot,
            CancellationToken cancellationToken = default
        )
        {
            BlobClient blobClient = SnapshotContainer.GetBlobClient(SnapshotPathEncoder.GetSnapshotPath(snapshotKey));
            _ = await blobClient.UploadAsync(
                SnapshotDocumentCodec.Encode(snapshotKey, snapshot),
                overwrite: true,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        private async IAsyncEnumerable<string> GetBlobNamesAsync(
            SnapshotStreamKey streamKey,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            string prefix = SnapshotPathEncoder.GetStreamPrefix(streamKey);

            if (ListBlobNamesAsyncOverride is not null)
            {
                await foreach (string blobName in ListBlobNamesAsyncOverride(prefix, cancellationToken).ConfigureAwait(false))
                {
                    yield return blobName;
                }

                yield break;
            }

            await foreach (Page<BlobItem> page in SnapshotContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, cancellationToken)
                               .AsPages(pageSizeHint: Options.ListPageSize)
                               .ConfigureAwait(false))
            {
                foreach (BlobItem blob in page.Values)
                {
                    yield return blob.Name;
                }
            }
        }

        private static bool TryParseVersionedBlob(
            string blobName,
            out VersionedBlob versionedBlob
        )
        {
            int lastSeparator = blobName.LastIndexOf('/');
            if (lastSeparator < 0)
            {
                versionedBlob = default!;
                return false;
            }

            ReadOnlySpan<char> fileName = blobName.AsSpan(lastSeparator + 1);
            if (!fileName.EndsWith(".json", StringComparison.Ordinal))
            {
                versionedBlob = default!;
                return false;
            }

            ReadOnlySpan<char> versionSpan = fileName[..^5];
            if (!long.TryParse(versionSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out long version))
            {
                versionedBlob = default!;
                return false;
            }

            versionedBlob = new(blobName, version);
            return true;
        }

        private sealed record VersionedBlob(
            string Name,
            long Version);
    }
}
