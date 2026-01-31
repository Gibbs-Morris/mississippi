namespace Mississippi.Sdk.Silo;

/// <summary>
///     Options for configuring Cosmos-backed Mississippi providers.
/// </summary>
public sealed class MississippiCosmosOptions
{
    /// <summary>
    ///     Gets or sets the Aspire connection name for Cosmos DB.
    /// </summary>
    public string ConnectionName { get; set; } = "cosmos-db";

    /// <summary>
    ///     Gets or sets the Aspire connection name for Azure Blob Storage locking.
    /// </summary>
    public string BlobConnectionName { get; set; } = "blobs";

    /// <summary>
    ///     Gets or sets the database identifier for Mississippi data.
    /// </summary>
    public string DatabaseId { get; set; } = "mississippi";

    /// <summary>
    ///     Gets or sets the Orleans stream provider name.
    /// </summary>
    public string StreamProviderName { get; set; } = "mississippi-streaming";

    /// <summary>
    ///     Gets or sets a prefix applied to Cosmos container names.
    /// </summary>
    public string ContainerPrefix { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether containers should be auto-created.
    /// </summary>
    public bool AutoCreateContainers { get; set; } = true;

    /// <summary>
    ///     Gets or sets the grain storage provider names.
    /// </summary>
    public StorageProviderNames StorageNames { get; set; } = new();
}
