using System;

using Azure.Storage.Blobs;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Tributary.Runtime.Storage.Azure;

/// <summary>
///     Resolves the keyed <see cref="BlobServiceClient" /> required by the Tributary Azure provider and translates missing
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
                $"Azure Tributary snapshot storage provider requires a keyed BlobServiceClient registered with service key '{blobServiceClientServiceKey}'. Register one before calling AddAzureSnapshotStorageProvider(), or use AddAzureSnapshotStorageProvider(connectionString, options => {{ ... }})."
            );
        }
    }
}
