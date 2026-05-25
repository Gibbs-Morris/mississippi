using System;
using System.Collections.Generic;

using Microsoft.Extensions.Options;


namespace Mississippi.Tributary.Runtime.Storage.Blobs;

/// <summary>
///     Validates <see cref="SnapshotBlobStorageOptions" /> values.
/// </summary>
internal sealed class SnapshotBlobStorageOptionsValidator : IValidateOptions<SnapshotBlobStorageOptions>
{
    private static bool IsLowercaseLetterOrDigit(
        char character
    ) =>
        ((character >= 'a') && (character <= 'z')) || ((character >= '0') && (character <= '9'));

    private static bool IsValidContainerName(
        string? value
    )
    {
        if (string.IsNullOrWhiteSpace(value) || (value.Length < 3) || (value.Length > 63))
        {
            return false;
        }

        if (!IsLowercaseLetterOrDigit(value[0]) || !IsLowercaseLetterOrDigit(value[^1]))
        {
            return false;
        }

        bool previousWasDash = false;
        foreach (char character in value)
        {
            if (IsLowercaseLetterOrDigit(character))
            {
                previousWasDash = false;
                continue;
            }

            if ((character != '-') || previousWasDash)
            {
                return false;
            }

            previousWasDash = true;
        }

        return true;
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(
        string? name,
        SnapshotBlobStorageOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        List<string> failures = [];
        if (string.IsNullOrWhiteSpace(options.BlobServiceClientServiceKey))
        {
            failures.Add("BlobServiceClientServiceKey must be provided.");
        }

        if (!IsValidContainerName(options.ContainerName))
        {
            failures.Add("ContainerName must be a valid Azure Blob container name.");
        }

        if (options.MaximumSnapshotPayloadSizeBytes <= 0)
        {
            failures.Add("MaximumSnapshotPayloadSizeBytes must be greater than zero.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}