using System;

using Azure.Storage.Blobs;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     Resolves the keyed <see cref="BlobServiceClient" /> required by the Brooks Azure provider and translates missing
///     registrations into consumer-facing guidance.
/// </summary>
internal static class AzureBlobServiceClientResolver
{
    /// <summary>
    ///     Resolves the configured keyed <see cref="BlobServiceClient" />.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve the keyed client.</param>
    /// <param name="blobServiceClientServiceKey">The keyed service registration name.</param>
    /// <returns>The resolved <see cref="BlobServiceClient" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider" /> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the keyed client has not been registered.</exception>
    internal static BlobServiceClient Resolve(
        IServiceProvider serviceProvider,
        string blobServiceClientServiceKey
    )
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        try
        {
            return serviceProvider.GetRequiredKeyedService<BlobServiceClient>(blobServiceClientServiceKey);
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Azure Brooks storage provider requires a keyed BlobServiceClient registered with service key '{blobServiceClientServiceKey}'. Register one before calling AddAzureBrookStorageProvider(), or use AddAzureBrookStorageProvider(connectionString, options => {{ ... }})."
            );
        }
    }
}