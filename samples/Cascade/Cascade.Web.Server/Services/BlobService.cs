using System;
using System.Text;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using BlobItemDto = Cascade.Web.Contracts.BlobItem;


namespace Cascade.Web.Server.Services;

/// <summary>
///     Blob Storage service implementation for connectivity testing.
/// </summary>
internal sealed class BlobService : IBlobService
{
    private const string ContainerName = "cascade-web-blobs";

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobService" /> class.
    /// </summary>
    /// <param name="blobServiceClient">The Blob Service client.</param>
    public BlobService(
        BlobServiceClient blobServiceClient
    ) =>
        BlobServiceClient = blobServiceClient;

    private BlobServiceClient BlobServiceClient { get; }

    /// <inheritdoc />
    public async Task<BlobItemDto?> GetBlobAsync(
        string name
    )
    {
        BlobContainerClient containerClient = BlobServiceClient.GetBlobContainerClient(ContainerName);
        if (!await containerClient.ExistsAsync())
        {
            return null;
        }

        BlobClient blobClient = containerClient.GetBlobClient(name);
        try
        {
            Response<BlobDownloadResult> response = await blobClient.DownloadContentAsync();
            return new()
            {
                Name = name,
                Content = response.Value.Content.ToString(),
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task UploadBlobAsync(
        BlobItemDto item
    )
    {
        BlobContainerClient containerClient = BlobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync();
        BlobClient blobClient = containerClient.GetBlobClient(item.Name);
        await blobClient.UploadAsync(new BinaryData(Encoding.UTF8.GetBytes(item.Content)), true);
    }
}