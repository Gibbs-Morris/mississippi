using System;

using Microsoft.Extensions.Options;


namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Validates <see cref="SnapshotBlobStorageOptions" /> values at startup.
/// </summary>
internal sealed class SnapshotBlobStorageOptionsValidator : IValidateOptions<SnapshotBlobStorageOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(
        string? name,
        SnapshotBlobStorageOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.BlobServiceClientServiceKey))
        {
            return ValidateOptionsResult.Fail($"{nameof(SnapshotBlobStorageOptions.BlobServiceClientServiceKey)} must be provided.");
        }

        if (string.IsNullOrWhiteSpace(options.ContainerName))
        {
            return ValidateOptionsResult.Fail($"{nameof(SnapshotBlobStorageOptions.ContainerName)} must be provided.");
        }

        return ValidateOptionsResult.Success;
    }
}
