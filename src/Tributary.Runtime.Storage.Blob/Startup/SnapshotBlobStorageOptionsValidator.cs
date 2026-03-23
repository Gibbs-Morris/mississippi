using System;
using System.Collections.Generic;

using Microsoft.Extensions.Options;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Startup;

/// <summary>
///     Validates required Blob snapshot storage options.
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

        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.ContainerName))
        {
            failures.Add("ContainerName must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.BlobServiceClientServiceKey))
        {
            failures.Add("BlobServiceClientServiceKey must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.PayloadSerializerFormat))
        {
            failures.Add("PayloadSerializerFormat must be configured.");
        }

        if (!Enum.IsDefined(options.ContainerInitializationMode))
        {
            failures.Add(
                $"ContainerInitializationMode value '{options.ContainerInitializationMode}' is not supported.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}