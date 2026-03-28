using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;


namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Implements a tiny in-memory Azure Blob REST surface for deterministic Brooks Azure L0 tests.
/// </summary>
internal sealed class InMemoryAzureBlobTransport
{
    private readonly Dictionary<string, StoredBlob> blobs = new(StringComparer.Ordinal);

    private readonly Dictionary<string, int> forcedAcquireConflicts = new(StringComparer.Ordinal);

    private int etagVersion = 1;

    /// <summary>
    ///     Seeds a blob payload directly into the fake Azure Blob store.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <param name="blobPath">The blob path relative to the container.</param>
    /// <param name="payload">The payload to store.</param>
    /// <param name="activeLeaseId">The active lease identifier, when applicable.</param>
    internal void SeedBlob(
        string containerName,
        string blobPath,
        BinaryData payload,
        string? activeLeaseId = null
    )
    {
        ArgumentNullException.ThrowIfNull(payload);

        blobs[CreateBlobKey(containerName, blobPath)] = new()
        {
            ActiveLeaseId = activeLeaseId,
            Content = payload.ToArray(),
            ETag = CreateNextEtag(),
        };
    }

    /// <summary>
    ///     Seeds a JSON blob directly into the fake Azure Blob store.
    /// </summary>
    /// <typeparam name="T">The JSON document type.</typeparam>
    /// <param name="containerName">The container name.</param>
    /// <param name="blobPath">The blob path relative to the container.</param>
    /// <param name="document">The document to serialize.</param>
    internal void SeedJson<T>(
        string containerName,
        string blobPath,
        T document
    ) => SeedBlob(containerName, blobPath, BinaryData.FromObjectAsJson(document));

    /// <summary>
    ///     Configures a lock blob to reject the next <c>AcquireLease</c> calls with conflict responses.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <param name="blobPath">The blob path relative to the container.</param>
    /// <param name="count">The number of acquire conflicts to force.</param>
    internal void SetLeaseAcquireConflicts(
        string containerName,
        string blobPath,
        int count
    ) => forcedAcquireConflicts[CreateBlobKey(containerName, blobPath)] = count;

    /// <summary>
    ///     Handles one fake Azure Blob REST request.
    /// </summary>
    /// <param name="request">The outbound SDK request.</param>
    /// <returns>The fake Azure Blob response.</returns>
    internal HttpResponseMessage Handle(
        HttpRequestMessage request
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        string blobKey = request.RequestUri?.AbsolutePath.TrimStart('/') ?? throw new InvalidOperationException("Request URI was missing.");
        string query = request.RequestUri?.Query ?? string.Empty;

        if (query.Contains("comp=lease", StringComparison.OrdinalIgnoreCase))
        {
            return HandleLeaseRequest(request, blobKey);
        }

        if (query.Contains("restype=container", StringComparison.OrdinalIgnoreCase))
        {
            return request.Method == HttpMethod.Put
                ? AzureInitializerTestContext.CreateResponse(HttpStatusCode.Created)
                : AzureInitializerTestContext.CreateResponse(HttpStatusCode.OK);
        }

        return request.Method.Method switch
        {
            "DELETE" => HandleDelete(request, blobKey),
            "GET" => HandleDownload(blobKey),
            "HEAD" => HandleExists(blobKey),
            "PUT" => HandleUpload(request, blobKey),
            _ => throw new InvalidOperationException($"Unsupported fake Azure Blob request '{request.Method}' for '{blobKey}'."),
        };
    }

    private static string CreateBlobKey(
        string containerName,
        string blobPath
    ) => $"{containerName}/{blobPath}";

    private static bool HasIfNoneMatchAll(
        HttpRequestMessage request
    ) => request.Headers.IfNoneMatch.Any(match => string.Equals(match.Tag, "*", StringComparison.Ordinal));

    private static bool MatchesIfMatch(
        HttpRequestMessage request,
        StoredBlob blob
    )
    {
        if (request.Headers.IfMatch.Count == 0)
        {
            return true;
        }

        return request.Headers.IfMatch.Any(match => string.Equals(match.Tag, blob.ETag, StringComparison.Ordinal));
    }

    private static HttpResponseMessage CreateBlobResponse(
        HttpStatusCode statusCode,
        StoredBlob blob,
        bool includeContent
    )
    {
        HttpResponseMessage response = AzureInitializerTestContext.CreateResponse(statusCode);
        response.Headers.ETag = EntityTagHeaderValue.Parse(blob.ETag);
        response.Headers.Add("x-ms-blob-type", "BlockBlob");

        if (includeContent)
        {
            response.Content = new ByteArrayContent(blob.Content);
            response.Content.Headers.ContentLength = blob.Content.Length;
            response.Content.Headers.ContentType = new("application/octet-stream");
        }

        return response;
    }

    private static HttpResponseMessage CreateErrorResponse(
        HttpStatusCode statusCode,
        string errorCode
    )
    {
        HttpResponseMessage response = AzureInitializerTestContext.CreateResponse(statusCode);
        response.Headers.Add("x-ms-error-code", errorCode);
        return response;
    }

    private string CreateNextEtag() => $"\"etag-{etagVersion++}\"";

    private HttpResponseMessage HandleDelete(
        HttpRequestMessage request,
        string blobKey
    )
    {
        if (!blobs.TryGetValue(blobKey, out StoredBlob? blob))
        {
            return CreateErrorResponse(HttpStatusCode.NotFound, "BlobNotFound");
        }

        if (!MatchesIfMatch(request, blob))
        {
            return CreateErrorResponse(HttpStatusCode.PreconditionFailed, "ConditionNotMet");
        }

        blobs.Remove(blobKey);
        return AzureInitializerTestContext.CreateResponse(HttpStatusCode.Accepted);
    }

    private HttpResponseMessage HandleDownload(
        string blobKey
    ) => blobs.TryGetValue(blobKey, out StoredBlob? blob)
        ? CreateBlobResponse(HttpStatusCode.OK, blob, includeContent: true)
        : CreateErrorResponse(HttpStatusCode.NotFound, "BlobNotFound");

    private HttpResponseMessage HandleExists(
        string blobKey
    ) => blobs.TryGetValue(blobKey, out StoredBlob? blob)
        ? CreateBlobResponse(HttpStatusCode.OK, blob, includeContent: false)
        : CreateErrorResponse(HttpStatusCode.NotFound, "BlobNotFound");

    private HttpResponseMessage HandleLeaseRequest(
        HttpRequestMessage request,
        string blobKey
    )
    {
        if (!blobs.TryGetValue(blobKey, out StoredBlob? blob))
        {
            return CreateErrorResponse(HttpStatusCode.NotFound, "BlobNotFound");
        }

        string action = request.Headers.GetValues("x-ms-lease-action").Single();
        string? requestedLeaseId = request.Headers.TryGetValues("x-ms-lease-id", out IEnumerable<string>? leaseIds)
            ? leaseIds.Single()
            : null;

        switch (action)
        {
            case "acquire":
                if (forcedAcquireConflicts.TryGetValue(blobKey, out int remainingConflicts) && (remainingConflicts > 0))
                {
                    forcedAcquireConflicts[blobKey] = remainingConflicts - 1;
                    return CreateErrorResponse(HttpStatusCode.Conflict, "LeaseAlreadyPresent");
                }

                if (!string.IsNullOrEmpty(blob.ActiveLeaseId))
                {
                    return CreateErrorResponse(HttpStatusCode.Conflict, "LeaseAlreadyPresent");
                }

                string acquiredLeaseId = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
                blob.ActiveLeaseId = acquiredLeaseId;
                HttpResponseMessage acquireResponse = CreateBlobResponse(HttpStatusCode.Created, blob, includeContent: false);
                acquireResponse.Headers.Add("x-ms-lease-id", acquiredLeaseId);
                return acquireResponse;

            case "release":
                if (!string.Equals(blob.ActiveLeaseId, requestedLeaseId, StringComparison.Ordinal))
                {
                    return CreateErrorResponse(HttpStatusCode.Conflict, "LeaseIdMismatchWithLeaseOperation");
                }

                blob.ActiveLeaseId = null;
                return AzureInitializerTestContext.CreateResponse(HttpStatusCode.OK);

            case "renew":
                if (!string.Equals(blob.ActiveLeaseId, requestedLeaseId, StringComparison.Ordinal))
                {
                    return CreateErrorResponse(HttpStatusCode.Conflict, "LeaseIdMismatchWithLeaseOperation");
                }

                HttpResponseMessage renewResponse = CreateBlobResponse(HttpStatusCode.OK, blob, includeContent: false);
                renewResponse.Headers.Add("x-ms-lease-id", requestedLeaseId);
                return renewResponse;

            default:
                throw new InvalidOperationException($"Unsupported fake lease action '{action}'.");
        }
    }

    private HttpResponseMessage HandleUpload(
        HttpRequestMessage request,
        string blobKey
    )
    {
        if (HasIfNoneMatchAll(request) && blobs.ContainsKey(blobKey))
        {
            return CreateErrorResponse(HttpStatusCode.PreconditionFailed, "ConditionNotMet");
        }

        if (blobs.TryGetValue(blobKey, out StoredBlob? currentBlob) && !MatchesIfMatch(request, currentBlob))
        {
            return CreateErrorResponse(HttpStatusCode.PreconditionFailed, "ConditionNotMet");
        }

        byte[] payload = ReadContentBytes(request.Content);

        StoredBlob nextBlob = currentBlob ?? new StoredBlob();
        nextBlob.Content = payload;
        nextBlob.ETag = CreateNextEtag();
        blobs[blobKey] = nextBlob;

        return CreateBlobResponse(HttpStatusCode.Created, nextBlob, includeContent: false);
    }

    private static byte[] ReadContentBytes(
        HttpContent? content
    )
    {
        if (content == null)
        {
            return [];
        }

        using MemoryStream stream = new();
        using Stream requestStream = content.ReadAsStream();
        requestStream.CopyTo(stream);
        return stream.ToArray();
    }

    private sealed class StoredBlob
    {
        internal string? ActiveLeaseId { get; set; }

        internal byte[] Content { get; set; } = [];

        internal string ETag { get; set; } = "\"etag-0\"";
    }
}