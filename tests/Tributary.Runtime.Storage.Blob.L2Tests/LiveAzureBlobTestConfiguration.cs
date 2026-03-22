using System;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L2Tests;

/// <summary>
///     Resolves opt-in live Azure Blob smoke-test configuration from environment variables.
/// </summary>
internal sealed class LiveAzureBlobTestConfiguration
{
    /// <summary>
    ///     The environment variable that supplies the live Azure Blob connection string.
    /// </summary>
    public const string ConnectionStringEnvironmentVariable = "MISSISSIPPI_TRIBUTARY_BLOB_LIVE_CONNECTION_STRING";

    /// <summary>
    ///     The environment variable that supplies the live Azure Blob container name.
    /// </summary>
    public const string ContainerEnvironmentVariable = "MISSISSIPPI_TRIBUTARY_BLOB_LIVE_CONTAINER";

    /// <summary>
    ///     Gets the live Azure Blob connection string.
    /// </summary>
    public required string ConnectionString { get; init; }

    /// <summary>
    ///     Gets the live Azure Blob container name.
    /// </summary>
    public required string ContainerName { get; init; }

    /// <summary>
    ///     Attempts to resolve live Azure Blob smoke-test configuration.
    /// </summary>
    /// <param name="configuration">The resolved live configuration when available.</param>
    /// <returns><see langword="true" /> when configuration is present; otherwise <see langword="false" />.</returns>
    public static bool TryCreate(
        out LiveAzureBlobTestConfiguration? configuration
    )
    {
        string? connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            configuration = null;
            return false;
        }

        configuration = new()
        {
            ConnectionString = connectionString,
            ContainerName = Environment.GetEnvironmentVariable(ContainerEnvironmentVariable) ?? "mississippi-tributary-live-tests",
        };
        return true;
    }
}
