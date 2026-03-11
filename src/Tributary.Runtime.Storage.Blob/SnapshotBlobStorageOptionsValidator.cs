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

        if (!IsValidContainerName(options.ContainerName))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(SnapshotBlobStorageOptions.ContainerName)} must be 3-63 characters, lowercase, " +
                "start and end with a letter or digit, use only letters, digits, and hyphens, and not contain consecutive hyphens.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsValidContainerName(
        string containerName
    )
    {
        if ((containerName.Length < 3) || (containerName.Length > 63))
        {
            return false;
        }

        if (!char.IsAsciiLetterOrDigit(containerName[0]) || !char.IsAsciiLetterOrDigit(containerName[^1]))
        {
            return false;
        }

        bool previousWasHyphen = false;
        foreach (char character in containerName)
        {
            bool isLowercaseLetter = character is >= 'a' and <= 'z';
            bool isDigit = character is >= '0' and <= '9';
            bool isHyphen = character == '-';

            if (!isLowercaseLetter && !isDigit && !isHyphen)
            {
                return false;
            }

            if (isHyphen && previousWasHyphen)
            {
                return false;
            }

            previousWasHyphen = isHyphen;
        }

        return true;
    }
}
