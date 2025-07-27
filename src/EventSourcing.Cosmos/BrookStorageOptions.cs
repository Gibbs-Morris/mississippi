namespace Mississippi.EventSourcing.Cosmos;

public class BrookStorageOptions
{
    public string DatabaseId { get; set; } = "mississippi";
    public string ContainerId { get; } = "brooks";
    public string LockContainerName { get; set; } = "stream-locks";
    public int QueryBatchSize { get; set; } = 100;
    public int MaxEventsPerBatch { get; set; } = 95;
    public int LeaseDurationSeconds { get; set; } = 60;
    public long MaxRequestSizeBytes { get; set; } = 1_800_000;
    public int LeaseRenewalThresholdSeconds { get; set; } = 20;
}