using System;

using Azure.Core.Pipeline;
using Azure.Storage;
using Azure.Storage.Blobs;

using Microsoft.Extensions.Options;


namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Owns a deterministic Azure Blob service client backed by the in-memory Brooks Azure test transport.
/// </summary>
internal sealed class AzureBlobTransportTestContext : IDisposable
{
    private readonly StorageSharedKeyCredential credential = new(
        "testaccount",
        Convert.ToBase64String(new byte[32]));

    /// <summary>
    ///     Initializes a new instance of the <see cref="AzureBlobTransportTestContext" /> class.
    /// </summary>
    /// <param name="options">The Brooks Azure options used by the runtime under test.</param>
    internal AzureBlobTransportTestContext(
        BrookStorageOptions? options = null
    )
    {
        StorageOptions = options ?? new BrookStorageOptions
        {
            BlobServiceClientServiceKey = "shared-account",
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

        BlobServiceClient = new(
            new Uri("https://testaccount.blob.core.windows.net/"),
            credential,
            blobClientOptions);
    }

    /// <summary>
    ///     Gets the deterministic Azure Blob service client under test.
    /// </summary>
    internal BlobServiceClient BlobServiceClient { get; }

    /// <summary>
    ///     Gets the HTTP request recorder used by the fake Azure transport.
    /// </summary>
    internal AzureTestHttpMessageHandler Handler { get; }

    /// <summary>
    ///     Gets the Brooks Azure options bound to this test context.
    /// </summary>
    internal BrookStorageOptions StorageOptions { get; }

    /// <summary>
    ///     Gets the in-memory Azure Blob transport state.
    /// </summary>
    internal InMemoryAzureBlobTransport Transport { get; }

    /// <summary>
    ///     Creates the <see cref="IOptions{TOptions}" /> wrapper used by Brooks Azure runtime services.
    /// </summary>
    /// <returns>The options wrapper.</returns>
    internal IOptions<BrookStorageOptions> CreateOptions() => Options.Create(StorageOptions);

    /// <inheritdoc />
    public void Dispose()
    {
        Handler.Dispose();
    }
}