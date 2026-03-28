using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Storage;

/// <summary>
///     Reads and writes Brooks Azure event-storage blobs and coordination documents.
/// </summary>
internal sealed class AzureBrookRepository : IAzureBrookRepository
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AzureBrookRepository" /> class.
    /// </summary>
    /// <param name="blobServiceClient">The Azure Blob service client.</param>
    /// <param name="options">The Brooks Azure storage options.</param>
    /// <param name="streamPathEncoder">The stream path encoder.</param>
    /// <param name="eventDocumentCodec">The event document codec.</param>
    public AzureBrookRepository(
        BlobServiceClient blobServiceClient,
        IOptions<BrookStorageOptions> options,
        IStreamPathEncoder streamPathEncoder,
        IAzureBrookEventDocumentCodec eventDocumentCodec
    )
    {
        BlobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        StreamPathEncoder = streamPathEncoder ?? throw new ArgumentNullException(nameof(streamPathEncoder));
        EventDocumentCodec = eventDocumentCodec ?? throw new ArgumentNullException(nameof(eventDocumentCodec));
    }

    private BlobServiceClient BlobServiceClient { get; }

    private BlobContainerClient BrooksContainer => BlobServiceClient.GetBlobContainerClient(Options.ContainerName);

    private IAzureBrookEventDocumentCodec EventDocumentCodec { get; }

    private BrookStorageOptions Options { get; }

    private IStreamPathEncoder StreamPathEncoder { get; }

    /// <inheritdoc />
    public async Task DeleteEventIfExistsAsync(
        BrookKey brookId,
        long position,
        CancellationToken cancellationToken = default
    )
    {
        BlobClient blobClient = BrooksContainer.GetBlobClient(StreamPathEncoder.GetEventPath(brookId, position));
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> EventExistsAsync(
        BrookKey brookId,
        long position,
        CancellationToken cancellationToken = default
    )
    {
        BlobClient blobClient = BrooksContainer.GetBlobClient(StreamPathEncoder.GetEventPath(brookId, position));
        Response<bool> exists = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
        return exists.Value;
    }

    /// <inheritdoc />
    public async Task<AzureBrookCommittedCursorState?> GetCommittedCursorAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    )
    {
        BlobClient blobClient = BrooksContainer.GetBlobClient(StreamPathEncoder.GetCursorPath(brookId));

        try
        {
            Response<BlobDownloadResult> response = await blobClient.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
            AzureBrookCommittedCursorState document = response.Value.Content.ToObjectFromJson<AzureBrookCommittedCursorState>() ?? throw new InvalidOperationException(
                "Brooks Azure cursor blob payload was empty.");
            return document with
            {
                ETag = response.Value.Details.ETag,
            };
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<AzureBrookPendingWriteState?> GetPendingWriteAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    )
    {
        BlobClient blobClient = BrooksContainer.GetBlobClient(StreamPathEncoder.GetPendingPath(brookId));

        try
        {
            Response<BlobDownloadResult> response = await blobClient.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
            AzureBrookPendingWriteState document = response.Value.Content.ToObjectFromJson<AzureBrookPendingWriteState>() ?? throw new InvalidOperationException(
                "Brooks Azure pending-state blob payload was empty.");
            return document with
            {
                ETag = response.Value.Details.ETag,
            };
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookRangeKey brookRange,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        BrookKey brookId = brookRange.ToBrookCompositeKey();
        for (long position = brookRange.Start.Value; position <= brookRange.End.Value; position++)
        {
            BlobClient blobClient = BrooksContainer.GetBlobClient(StreamPathEncoder.GetEventPath(brookId, position));

            Response<BlobDownloadResult> response;
            try
            {
                response = await blobClient.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException exception) when (exception.Status == 404)
            {
                throw new InvalidOperationException(
                    $"Brooks Azure expected committed event blob at position {position} for brook '{brookId}', but the blob was missing.",
                    exception);
            }

            yield return EventDocumentCodec.Decode(response.Value.Content);
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryAdvanceCommittedCursorAsync(
        BrookKey brookId,
        AzureBrookCommittedCursorState? cursor,
        AzureBrookPendingWriteState pendingWrite,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(pendingWrite);

        BlobClient blobClient = BrooksContainer.GetBlobClient(StreamPathEncoder.GetCursorPath(brookId));
        BlobRequestConditions conditions = cursor == null
            ? new()
            {
                IfNoneMatch = ETag.All,
            }
            : new()
            {
                IfMatch = cursor.ETag,
            };

        AzureBrookCommittedCursorState nextCursor = new()
        {
            Position = pendingWrite.TargetPosition,
        };

        try
        {
            await blobClient.UploadAsync(
                    BinaryData.FromObjectAsJson(nextCursor),
                    new BlobUploadOptions
                    {
                        Conditions = conditions,
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (RequestFailedException exception) when ((exception.Status == 409) || (exception.Status == 412))
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryCreatePendingWriteAsync(
        BrookKey brookId,
        AzureBrookPendingWriteState pendingWrite,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(pendingWrite);

        BlobClient blobClient = BrooksContainer.GetBlobClient(StreamPathEncoder.GetPendingPath(brookId));

        try
        {
            await blobClient.UploadAsync(
                    BinaryData.FromObjectAsJson(pendingWrite),
                    new BlobUploadOptions
                    {
                        Conditions = new BlobRequestConditions
                        {
                            IfNoneMatch = ETag.All,
                        },
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (RequestFailedException exception) when ((exception.Status == 409) || (exception.Status == 412))
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryDeletePendingWriteAsync(
        BrookKey brookId,
        AzureBrookPendingWriteState pendingWrite,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(pendingWrite);

        BlobClient blobClient = BrooksContainer.GetBlobClient(StreamPathEncoder.GetPendingPath(brookId));

        try
        {
            Response<bool> response = await blobClient.DeleteIfExistsAsync(
                    conditions: new BlobRequestConditions
                    {
                        IfMatch = pendingWrite.ETag,
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return response.Value;
        }
        catch (RequestFailedException exception) when ((exception.Status == 404) || (exception.Status == 409) || (exception.Status == 412))
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task WriteEventAsync(
        BrookKey brookId,
        long position,
        BrookEvent brookEvent,
        CancellationToken cancellationToken = default
    )
    {
        BlobClient blobClient = BrooksContainer.GetBlobClient(StreamPathEncoder.GetEventPath(brookId, position));
        await blobClient.UploadAsync(
                EventDocumentCodec.Encode(brookEvent, position),
                new BlobUploadOptions
                {
                    Conditions = new BlobRequestConditions
                    {
                        IfNoneMatch = ETag.All,
                    },
                },
                cancellationToken)
            .ConfigureAwait(false);
    }
}