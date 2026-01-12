using System.Threading.Tasks;

using BlobItemDto = Cascade.Contracts.BlobItem;


namespace Cascade.Server.Services;

/// <summary>
///     Service for Blob Storage operations.
/// </summary>
internal interface IBlobService
{
    /// <summary>
    ///     Gets a blob by name.
    /// </summary>
    /// <param name="name">The blob name.</param>
    /// <returns>The blob item, or null if not found.</returns>
    Task<BlobItemDto?> GetBlobAsync(
        string name
    );

    /// <summary>
    ///     Uploads a blob.
    /// </summary>
    /// <param name="item">The blob item to upload.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UploadBlobAsync(
        BlobItemDto item
    );
}