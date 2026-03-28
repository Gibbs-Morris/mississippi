using System;

using Azure;


namespace Mississippi.Tributary.Runtime.Storage.Azure
{
    /// <summary>
    ///     Formats consumer-facing Azure diagnostics without leaking connection data or raw blob URIs.
    /// </summary>
    internal static class AzureDiagnosticsRedaction
    {
        /// <summary>
        ///     Creates a safe, actionable exception for Azure container validation failures.
        /// </summary>
        /// <param name="containerName">The configured container name.</param>
        /// <param name="blobServiceClientServiceKey">The keyed client registration used for resolution.</param>
        /// <param name="initializationMode">The startup initialization mode in effect.</param>
        /// <param name="exception">The Azure SDK exception.</param>
        /// <returns>A sanitized <see cref="InvalidOperationException" />.</returns>
        internal static InvalidOperationException CreateContainerAccessException(
            string containerName,
            string blobServiceClientServiceKey,
            SnapshotStorageInitializationMode initializationMode,
            RequestFailedException exception
        )
        {
            string prefix =
                $"Azure Tributary snapshot storage provider failed to validate the snapshot container '{containerName}' using BlobServiceClient service key '{blobServiceClientServiceKey}' while running in {initializationMode} mode.";

            return exception.Status switch
            {
                401 or 403 => new InvalidOperationException(
                    $"{prefix} The configured identity does not have the required Azure Blob Storage permissions. Azure returned {Describe(exception)}."),
                404 => new InvalidOperationException(
                    $"{prefix} The container was not found. Create the container or switch InitializationMode to ValidateOrCreate."),
                _ => new InvalidOperationException($"{prefix} Azure returned {Describe(exception)}."),
            };
        }

        /// <summary>
        ///     Converts a request failure into a sanitized status summary.
        /// </summary>
        /// <param name="exception">The Azure SDK exception.</param>
        /// <returns>A sanitized status description.</returns>
        internal static string Describe(
            RequestFailedException exception
        )
        {
            return string.IsNullOrWhiteSpace(exception.ErrorCode)
                ? $"status {exception.Status}"
                : $"status {exception.Status} ({exception.ErrorCode})";
        }
    }
}
