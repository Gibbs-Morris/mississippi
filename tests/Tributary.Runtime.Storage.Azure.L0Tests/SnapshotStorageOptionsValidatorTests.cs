using System;
using System.Linq;

using Microsoft.Extensions.Options;

using Mississippi.Tributary.Runtime.Storage.Azure;

namespace MississippiTests.Tributary.Runtime.Storage.Azure.L0Tests
{
    /// <summary>
    ///     Tests for <see cref="SnapshotStorageOptionsValidator" />.
    /// </summary>
    public sealed class SnapshotStorageOptionsValidatorTests
    {
        /// <summary>
        ///     Invalid keyed-client configuration, reserved container names, and invalid page sizes are rejected.
        /// </summary>
        [Fact]
        public void ValidateRejectsReservedContainersAndInvalidNumericConfiguration()
        {
            SnapshotStorageOptions options = new()
            {
                BlobServiceClientServiceKey = string.Empty,
                ContainerName = SnapshotAzureDefaults.ReservedBrooksContainerName,
                ListPageSize = 0,
            };
            SnapshotStorageOptionsValidator validator = new();

            ValidateOptionsResult result = validator.Validate(name: null, options);
            string[] failures = result.Failures?.ToArray() ?? [];

            Assert.False(result.Succeeded);
            Assert.Contains(failures, failure => failure.Contains(nameof(SnapshotStorageOptions.BlobServiceClientServiceKey), StringComparison.Ordinal));
            Assert.Contains(failures, failure => failure.Contains(SnapshotAzureDefaults.ReservedBrooksContainerName, StringComparison.Ordinal));
            Assert.Contains(failures, failure => failure.Contains(nameof(SnapshotStorageOptions.ListPageSize), StringComparison.Ordinal));
        }

        /// <summary>
        ///     The shared-account Brooks lock container name is reserved for lock blobs and cannot be reused for snapshots.
        /// </summary>
        [Fact]
        public void ValidateRejectsTheReservedLockContainerName()
        {
            SnapshotStorageOptions options = new()
            {
                BlobServiceClientServiceKey = "shared-account",
                ContainerName = SnapshotAzureDefaults.ReservedLockContainerName,
                ListPageSize = 25,
            };
            SnapshotStorageOptionsValidator validator = new();

            ValidateOptionsResult result = validator.Validate(name: null, options);
            string[] failures = result.Failures?.ToArray() ?? [];

            Assert.False(result.Succeeded);
            Assert.Contains(failures, failure => failure.Contains(SnapshotAzureDefaults.ReservedLockContainerName, StringComparison.Ordinal));
        }

        /// <summary>
        ///     Invalid Azure container names are rejected before any network call can occur.
        /// </summary>
        [Fact]
        public void ValidateRejectsInvalidContainerNames()
        {
            SnapshotStorageOptions options = new()
            {
                BlobServiceClientServiceKey = "shared-account",
                ContainerName = "Invalid-Uppercase",
                ListPageSize = 25,
            };
            SnapshotStorageOptionsValidator validator = new();

            ValidateOptionsResult result = validator.Validate(name: null, options);
            string[] failures = result.Failures?.ToArray() ?? [];

            Assert.False(result.Succeeded);
            string failure = Assert.Single(failures, item => item.Contains(nameof(SnapshotStorageOptions.ContainerName), StringComparison.Ordinal));
            Assert.Contains("invalid", failure, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("lowercase letters", failure, StringComparison.OrdinalIgnoreCase);
        }
    }
}
