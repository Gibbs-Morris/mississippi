namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     Configuration options for the Azure Blob Storage Brooks provider.
/// </summary>
public sealed class BrookStorageOptions
{
    /// <summary>
    ///     Gets or sets the keyed service key used to resolve the <c>BlobServiceClient</c> from DI.
    /// </summary>
    public string BlobServiceClientServiceKey { get; set; } = BrookAzureDefaults.BlobServiceClientServiceKey;

    /// <summary>
    ///     Gets or sets the Brooks event container name.
    /// </summary>
    public string ContainerName { get; set; } = BrookAzureDefaults.ContainerName;

    /// <summary>
    ///     Gets or sets the startup initialization behavior.
    /// </summary>
    public BrookStorageInitializationMode InitializationMode { get; set; } =
        BrookStorageInitializationMode.ValidateOrCreate;

    /// <summary>
    ///     Gets or sets the lease duration in seconds for same-stream write serialization.
    /// </summary>
    public int LeaseDurationSeconds { get; set; } = 60;

    /// <summary>
    ///     Gets or sets the threshold in seconds for lease renewal operations.
    /// </summary>
    public int LeaseRenewalThresholdSeconds { get; set; } = 20;

    /// <summary>
    ///     Gets or sets the lock container name.
    /// </summary>
    public string LockContainerName { get; set; } = BrookAzureDefaults.LockContainerName;

    /// <summary>
    ///     Gets or sets the maximum number of events per append batch.
    /// </summary>
    public int MaxEventsPerBatch { get; set; } = 90;

    /// <summary>
    ///     Gets or sets the bounded read prefetch count for ordered range reads.
    /// </summary>
    public int ReadPrefetchCount { get; set; } = 16;
}