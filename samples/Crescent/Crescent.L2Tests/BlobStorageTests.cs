// <copyright file="BlobStorageTests.cs" company="Gibbs-Morris LLC">
// Licensed under the Gibbs-Morris commercial license.
// </copyright>

using Azure;
using Azure.Storage.Blobs.Models;


namespace Crescent.L2Tests;

/// <summary>
///     Integration tests for Azure Blob Storage operations using the Crescent Azurite emulator.
/// </summary>
[Collection(CrescentTestCollection.Name)]
#pragma warning disable CA1515 // Types can be made internal - xUnit test class must be public
public sealed class BlobStorageTests
#pragma warning restore CA1515
{
    private const string TestContainerName = "test-container";

    private readonly CrescentFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobStorageTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Aspire fixture.</param>
    public BlobStorageTests(
        CrescentFixture fixture
    ) =>
        this.fixture = fixture;

    /// <summary>
    ///     Verifies that a blob can be deleted.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteBlobShouldRemoveBlob()
    {
        // Arrange
        BlobServiceClient blobServiceClient = fixture.CreateBlobServiceClient();
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(TestContainerName);
        await containerClient.CreateIfNotExistsAsync();
        string blobName = $"delete-test-{Guid.NewGuid()}.txt";
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        // Upload the blob first
        using MemoryStream stream = new(Encoding.UTF8.GetBytes("Content to delete"));
        await blobClient.UploadAsync(stream, true);

        // Verify it exists
        bool existsBefore = await blobClient.ExistsAsync();
        existsBefore.Should().BeTrue("the blob should exist before deletion");

        // Act
        await blobClient.DeleteAsync();

        // Assert
        bool existsAfter = await blobClient.ExistsAsync();
        existsAfter.Should().BeFalse("the blob should not exist after deletion");
    }

    /// <summary>
    ///     Verifies that blobs can be listed in a container.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ListBlobsShouldReturnUploadedBlobs()
    {
        // Arrange
        string uniquePrefix = Guid.NewGuid().ToString("N")[..8];
        BlobServiceClient blobServiceClient = fixture.CreateBlobServiceClient();

        // Use a unique container for this test to avoid interference
        string containerName = $"list-test-{uniquePrefix}";
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        // Upload multiple blobs
        List<string> uploadedBlobNames = new();
        for (int i = 0; i < 3; i++)
        {
            string blobName = $"{uniquePrefix}/blob-{i}.txt";
            uploadedBlobNames.Add(blobName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            using MemoryStream stream = new(Encoding.UTF8.GetBytes($"Content {i}"));
            await blobClient.UploadAsync(stream, true);
        }

        // Act
        List<string> listedBlobNames = new();
        await foreach (BlobItem blobItem in containerClient.GetBlobsAsync(prefix: uniquePrefix))
        {
            listedBlobNames.Add(blobItem.Name);
        }

        // Assert
        listedBlobNames.Should().HaveCount(3, "we uploaded 3 blobs with the matching prefix");
        listedBlobNames.Should().BeEquivalentTo(uploadedBlobNames, "the listed blobs should match the uploaded blobs");

        // Cleanup
        await containerClient.DeleteIfExistsAsync();
    }

    /// <summary>
    ///     Verifies that a file can be read from blob storage.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReadBlobShouldReturnWrittenContent()
    {
        // Arrange
        BlobServiceClient blobServiceClient = fixture.CreateBlobServiceClient();
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(TestContainerName);
        await containerClient.CreateIfNotExistsAsync();
        string blobName = $"read-test-{Guid.NewGuid()}.txt";
        string expectedContent = $"Test content written at {DateTime.UtcNow:O}";
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        // Write the blob first
        using MemoryStream uploadStream = new(Encoding.UTF8.GetBytes(expectedContent));
        await blobClient.UploadAsync(uploadStream, true);

        // Act
        AzureBlobDownloadResult downloadResponse = await blobClient.DownloadContentAsync();
        string actualContent = downloadResponse.Value.Content.ToString();

        // Assert
        actualContent.Should().Be(expectedContent, "the downloaded content should match what was uploaded");
    }

    /// <summary>
    ///     Verifies that reading a non-existent blob throws an appropriate exception.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReadNonExistentBlobShouldThrowNotFoundException()
    {
        // Arrange
        BlobServiceClient blobServiceClient = fixture.CreateBlobServiceClient();
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(TestContainerName);
        await containerClient.CreateIfNotExistsAsync();
        string nonExistentBlobName = $"non-existent-{Guid.NewGuid()}.txt";
        BlobClient blobClient = containerClient.GetBlobClient(nonExistentBlobName);

        // Act
        Func<Task> act = async () => await blobClient.DownloadContentAsync();

        // Assert
        await act.Should().ThrowAsync<RequestFailedException>("the blob does not exist").Where(e => e.Status == 404);
    }

    /// <summary>
    ///     Verifies that binary content can be stored and retrieved.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task WriteBinaryBlobShouldPreserveContent()
    {
        // Arrange
        BlobServiceClient blobServiceClient = fixture.CreateBlobServiceClient();
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(TestContainerName);
        await containerClient.CreateIfNotExistsAsync();
        string blobName = $"binary-test-{Guid.NewGuid()}.bin";
        byte[] expectedContent = new byte[1024];
#pragma warning disable CA5394 // Random is insecure - acceptable for test data generation
        Random.Shared.NextBytes(expectedContent);
#pragma warning restore CA5394
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        // Act - Upload
        using MemoryStream uploadStream = new(expectedContent);
        await blobClient.UploadAsync(uploadStream, true);

        // Act - Download
        AzureBlobDownloadResult downloadResponse = await blobClient.DownloadContentAsync();
        byte[] actualContent = downloadResponse.Value.Content.ToArray();

        // Assert
        actualContent.Should().BeEquivalentTo(expectedContent, "binary content should be preserved exactly");
    }

    /// <summary>
    ///     Verifies that a file can be written to blob storage.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task WriteBlobShouldSucceed()
    {
        // Arrange
        BlobServiceClient blobServiceClient = fixture.CreateBlobServiceClient();
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(TestContainerName);
        await containerClient.CreateIfNotExistsAsync();
        string blobName = $"test-blob-{Guid.NewGuid()}.txt";
        string content = "Hello, Aspire Blob Storage!";
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        // Act
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(content));
        AzureBlobContentInfo response = await blobClient.UploadAsync(stream, true);

        // Assert
        response.Value.Should().NotBeNull("the upload should return content info");
#pragma warning disable IDISP004 // Don't ignore created IDisposable - GetRawResponse returns wrapper that doesn't need disposal
        response.GetRawResponse().Status.Should().Be(201, "HTTP 201 Created indicates successful upload");
#pragma warning restore IDISP004

        // Verify the blob exists
        bool exists = await blobClient.ExistsAsync();
        exists.Should().BeTrue("the blob should exist after upload");
    }
}