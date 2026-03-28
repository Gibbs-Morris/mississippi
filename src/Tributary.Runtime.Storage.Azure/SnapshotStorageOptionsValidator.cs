using System;
using System.Collections.Generic;

using Microsoft.Extensions.Options;


namespace Mississippi.Tributary.Runtime.Storage.Azure
{
    /// <summary>
    ///     Validates Tributary Azure snapshot storage options before host startup completes.
    /// </summary>
    internal sealed class SnapshotStorageOptionsValidator : IValidateOptions<SnapshotStorageOptions>
    {
        /// <inheritdoc />
        public ValidateOptionsResult Validate(
            string? name,
            SnapshotStorageOptions options
        )
        {
            ArgumentNullException.ThrowIfNull(options);

            List<string> failures = [];

            if (string.IsNullOrWhiteSpace(options.BlobServiceClientServiceKey))
            {
                failures.Add(
                    "Azure Tributary snapshot storage provider requires SnapshotStorageOptions.BlobServiceClientServiceKey to be configured with the keyed BlobServiceClient to use.");
            }

            ValidateContainerName(failures, options.ContainerName, nameof(SnapshotStorageOptions.ContainerName));

            if (string.Equals(options.ContainerName, SnapshotAzureDefaults.ReservedBrooksContainerName, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add(
                    $"Azure Tributary snapshot storage provider reserves the container name '{SnapshotAzureDefaults.ReservedBrooksContainerName}' for Brooks event blobs in the shared-account topology. Choose a different SnapshotStorageOptions.ContainerName value.");
            }

            if (string.Equals(options.ContainerName, SnapshotAzureDefaults.ReservedLockContainerName, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add(
                    $"Azure Tributary snapshot storage provider reserves the container name '{SnapshotAzureDefaults.ReservedLockContainerName}' for Brooks lock blobs in the shared-account topology. Choose a different SnapshotStorageOptions.ContainerName value.");
            }

            if (options.ListPageSize <= 0)
            {
                failures.Add("Azure Tributary snapshot storage provider requires ListPageSize to be greater than zero.");
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
                $"Azure Tributary snapshot storage provider received invalid {optionName} value '{containerName}'. {failure}");
        }
    }
}
