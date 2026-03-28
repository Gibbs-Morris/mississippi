using System;
using System.Collections.Generic;

using Microsoft.Extensions.Options;


namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     Validates Brooks Azure storage options before host startup completes.
/// </summary>
internal sealed class BrookStorageOptionsValidator : IValidateOptions<BrookStorageOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(
        string? name,
        BrookStorageOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.BlobServiceClientServiceKey))
        {
            failures.Add(
                "Azure Brooks storage provider requires BrookStorageOptions.BlobServiceClientServiceKey to be configured with the keyed BlobServiceClient to use.");
        }

        ValidateContainerName(failures, options.ContainerName, nameof(BrookStorageOptions.ContainerName));
        ValidateContainerName(failures, options.LockContainerName, nameof(BrookStorageOptions.LockContainerName));

        if (string.Equals(options.ContainerName, options.LockContainerName, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(
                "Azure Brooks storage provider requires BrookStorageOptions.ContainerName and LockContainerName to be different so event and lease blobs do not share a container.");
        }

        if (string.Equals(options.ContainerName, BrookAzureDefaults.SnapshotContainerName, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(
                $"Azure Brooks storage provider reserves the container name '{BrookAzureDefaults.SnapshotContainerName}' for Tributary snapshots in the shared-account topology. Choose a different BrookStorageOptions.ContainerName value.");
        }

        if (string.Equals(options.LockContainerName, BrookAzureDefaults.SnapshotContainerName, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(
                $"Azure Brooks storage provider reserves the container name '{BrookAzureDefaults.SnapshotContainerName}' for Tributary snapshots in the shared-account topology. Choose a different BrookStorageOptions.LockContainerName value.");
        }

        if (options.LeaseDurationSeconds <= 0)
        {
            failures.Add("Azure Brooks storage provider requires LeaseDurationSeconds to be greater than zero.");
        }

        if (options.LeaseRenewalThresholdSeconds <= 0)
        {
            failures.Add("Azure Brooks storage provider requires LeaseRenewalThresholdSeconds to be greater than zero.");
        }

        if (options.LeaseRenewalThresholdSeconds >= options.LeaseDurationSeconds)
        {
            failures.Add(
                "Azure Brooks storage provider requires LeaseRenewalThresholdSeconds to be less than LeaseDurationSeconds so lease renewal can happen before the lease expires.");
        }

        if (options.MaxEventsPerBatch <= 0)
        {
            failures.Add("Azure Brooks storage provider requires MaxEventsPerBatch to be greater than zero.");
        }

        if (options.ReadPrefetchCount <= 0)
        {
            failures.Add("Azure Brooks storage provider requires ReadPrefetchCount to be greater than zero.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateContainerName(
        List<string> failures,
        string containerName,
        string optionName
    )
    {
        if (AzureBlobContainerNameValidator.TryValidate(containerName, out string failure))
        {
            return;
        }

        failures.Add(
            $"Azure Brooks storage provider received invalid {optionName} value '{containerName}'. {failure}");
    }
}