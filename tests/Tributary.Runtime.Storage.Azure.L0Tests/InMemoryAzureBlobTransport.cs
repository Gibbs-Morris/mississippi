using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MississippiTests.Tributary.Runtime.Storage.Azure.L0Tests
{
    /// <summary>
    ///     Implements a tiny in-memory Azure Blob REST surface for deterministic Tributary Azure L0 tests.
    /// </summary>
    internal sealed class InMemoryAzureBlobTransport
    {
        private readonly Dictionary<string, StoredBlob> blobs = new(StringComparer.Ordinal);

        private readonly Dictionary<string, int> forcedAcquireConflicts = new(StringComparer.Ordinal);

        private int etagVersion = 1;

        /// <summary>
        ///     Gets a value indicating whether the specified blob exists.
        /// </summary>
        /// <param name="containerName">The container name.</param>
        /// <param name="blobPath">The blob path relative to the container.</param>
        /// <returns><c>true</c> when the blob exists; otherwise, <c>false</c>.</returns>
        internal bool BlobExists(
            string containerName,
            string blobPath
        )
        {
            return blobs.ContainsKey(CreateBlobKey(containerName, blobPath));
        }

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

            Uri requestUri = request.RequestUri ?? throw new InvalidOperationException("Request URI was missing.");
            string absolutePath = requestUri.AbsolutePath.TrimStart('/');

            if (requestUri.Query.Contains("comp=lease", StringComparison.OrdinalIgnoreCase))
            {
                return HandleLeaseRequest(request, absolutePath);
            }

            if (requestUri.Query.Contains("restype=container", StringComparison.OrdinalIgnoreCase))
            {
                bool isListRequest = requestUri.Query.Contains("comp=list", StringComparison.OrdinalIgnoreCase);

                return isListRequest switch
                {
                    true => HandleList(requestUri, absolutePath),
                    false => request.Method.Method switch
                    {
                        "PUT" => CreateResponse(HttpStatusCode.Created),
                        _ => CreateResponse(HttpStatusCode.OK),
                    },
                };
            }

            return request.Method.Method switch
            {
                "DELETE" => HandleDelete(request, absolutePath),
                "GET" => HandleDownload(absolutePath),
                "HEAD" => HandleExists(absolutePath),
                "PUT" => HandleUpload(request, absolutePath),
                _ => throw new InvalidOperationException($"Unsupported fake Azure Blob request '{request.Method}' for '{absolutePath}'."),
            };
        }

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

            blobs[CreateBlobKey(containerName, blobPath)] = new StoredBlob
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
        )
        {
            SeedBlob(containerName, blobPath, BinaryData.FromObjectAsJson(document));
        }

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
        )
        {
            forcedAcquireConflicts[CreateBlobKey(containerName, blobPath)] = count;
        }

        /// <summary>
        ///     Lists blob names under the requested container and prefix.
        /// </summary>
        /// <param name="containerName">The container to inspect.</param>
        /// <param name="prefix">The blob name prefix to match.</param>
        /// <param name="cancellationToken">The enumeration cancellation token.</param>
        /// <returns>The matching blob names in deterministic ordinal order.</returns>
        internal async IAsyncEnumerable<string> ListBlobNamesAsync(
            string containerName,
            string prefix,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            string containerPrefix = containerName + "/";
            string[] matches =
            [..
                blobs.Keys
                    .Where(key => key.StartsWith(containerPrefix, StringComparison.Ordinal))
                    .Select(key => key[containerPrefix.Length..])
                    .Where(blobName => blobName.StartsWith(prefix, StringComparison.Ordinal))
                    .OrderBy(blobName => blobName, StringComparer.Ordinal),
            ];

            foreach (string match in matches)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return match;
                await Task.CompletedTask;
            }
        }

        private static string CreateBlobKey(
            string containerName,
            string blobPath
        )
        {
            return $"{containerName}/{blobPath}";
        }

        private static HttpResponseMessage CreateBlobResponse(
            HttpStatusCode statusCode,
            StoredBlob blob,
            bool includeContent
        )
        {
            HttpResponseMessage response = CreateResponse(statusCode);
            response.Headers.ETag = EntityTagHeaderValue.Parse(blob.ETag);
            response.Headers.Add("x-ms-blob-type", "BlockBlob");

            if (includeContent)
            {
                response.Content = new ByteArrayContent(blob.Content);
                response.Content.Headers.ContentLength = blob.Content.Length;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            }

            return response;
        }

        private static HttpResponseMessage CreateErrorResponse(
            HttpStatusCode statusCode,
            string errorCode
        )
        {
            HttpResponseMessage response = CreateResponse(statusCode);
            response.Headers.Add("x-ms-error-code", errorCode);
            return response;
        }

        private static HttpResponseMessage CreateResponse(
            HttpStatusCode statusCode
        )
        {
            HttpResponseMessage response = new(statusCode);
            response.Headers.Add("x-ms-request-id", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
            response.Headers.Add("x-ms-version", "2025-11-05");
            response.Headers.Date = DateTimeOffset.UtcNow;
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentLength = 0;
            return response;
        }

        private static string? GetQueryParameter(
            Uri requestUri,
            string name
        )
        {
            string query = requestUri.Query;
            if (string.IsNullOrEmpty(query))
            {
                return null;
            }

            foreach (string segment in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] pieces = segment.Split('=', 2);
                if (!string.Equals(Uri.UnescapeDataString(pieces[0]), name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return pieces.Length == 2 ? Uri.UnescapeDataString(pieces[1]) : string.Empty;
            }

            return null;
        }

        private static bool HasIfNoneMatchAll(
            HttpRequestMessage request
        )
        {
            return request.Headers.IfNoneMatch.Any(match => string.Equals(match.Tag, "*", StringComparison.Ordinal));
        }

        private static bool MatchesIfMatch(
            HttpRequestMessage request,
            StoredBlob blob
        )
        {
            return request.Headers.IfMatch.Count == 0 || request.Headers.IfMatch.Any(match => string.Equals(match.Tag, blob.ETag, StringComparison.Ordinal));
        }

        private string CreateNextEtag()
        {
            return $"\"etag-{etagVersion++}\"";
        }

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

            _ = blobs.Remove(blobKey);
            return CreateResponse(HttpStatusCode.Accepted);
        }

        private HttpResponseMessage HandleDownload(
            string blobKey
        )
        {
            return blobs.TryGetValue(blobKey, out StoredBlob? blob)
                ? CreateBlobResponse(HttpStatusCode.OK, blob, includeContent: true)
                : CreateErrorResponse(HttpStatusCode.NotFound, "BlobNotFound");
        }

        private HttpResponseMessage HandleExists(
            string blobKey
        )
        {
            return blobs.TryGetValue(blobKey, out StoredBlob? blob)
                ? CreateBlobResponse(HttpStatusCode.OK, blob, includeContent: false)
                : CreateErrorResponse(HttpStatusCode.NotFound, "BlobNotFound");
        }

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

                    string acquiredLeaseId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
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
                    return CreateResponse(HttpStatusCode.OK);

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

        private HttpResponseMessage HandleList(
            Uri requestUri,
            string containerName
        )
        {
            string prefix = GetQueryParameter(requestUri, "prefix") ?? string.Empty;
            int maxResults = int.TryParse(GetQueryParameter(requestUri, "maxresults"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : int.MaxValue;

            (string BlobName, StoredBlob Blob)[] matchingBlobs =
            [..
                blobs
                    .Where(pair => pair.Key.StartsWith(containerName + "/", StringComparison.Ordinal))
                    .Select(pair => (BlobName: pair.Key[(containerName.Length + 1)..], Blob: pair.Value))
                    .Where(pair => pair.BlobName.StartsWith(prefix, StringComparison.Ordinal))
                    .OrderBy(pair => pair.BlobName, StringComparer.Ordinal)
                    .Take(maxResults)
            ];

            XNamespace ns = "http://schemas.microsoft.com/windowsazure";
            XElement xml = new(
                ns + "EnumerationResults",
                new XAttribute("ServiceEndpoint", "https://testaccount.blob.core.windows.net/"),
                new XAttribute("ContainerName", containerName),
                new XElement(ns + "Prefix", prefix),
                new XElement(ns + "MaxResults", maxResults == int.MaxValue ? string.Empty : maxResults.ToString(CultureInfo.InvariantCulture)),
                new XElement(
                    ns + "Blobs",
                    matchingBlobs.Select(pair => new XElement(
                        ns + "Blob",
                        new XElement(ns + "Name", pair.BlobName),
                        new XElement(
                            ns + "Properties",
                            new XElement(ns + "Content-Length", pair.Blob.Content.Length.ToString(CultureInfo.InvariantCulture)),
                            new XElement(ns + "Content-Type", "application/octet-stream"),
                            new XElement(ns + "Etag", pair.Blob.ETag),
                            new XElement(ns + "BlobType", "BlockBlob"))))),
                new XElement(ns + "NextMarker", string.Empty));

            HttpResponseMessage response = CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(
                new XDocument(new XDeclaration("1.0", "utf-8", null), xml).ToString(),
                Encoding.UTF8,
                "application/xml");
            return response;
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
}
