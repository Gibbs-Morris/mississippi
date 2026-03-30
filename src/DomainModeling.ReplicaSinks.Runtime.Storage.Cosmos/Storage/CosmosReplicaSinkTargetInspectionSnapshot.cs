namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.Storage;

/// <summary>
///     Represents the aggregate inspection summary produced from Cosmos target documents.
/// </summary>
internal sealed class CosmosReplicaSinkTargetInspectionSnapshot
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosReplicaSinkTargetInspectionSnapshot" /> class.
    /// </summary>
    /// <param name="targetExists">A value indicating whether the target marker exists.</param>
    /// <param name="writeCount">The total number of applied writes observed for the target.</param>
    /// <param name="latestSourcePosition">The latest applied source position, when present.</param>
    /// <param name="latestPayload">The latest applied payload, when present.</param>
    public CosmosReplicaSinkTargetInspectionSnapshot(
        bool targetExists,
        long writeCount,
        long? latestSourcePosition = null,
        object? latestPayload = null
    )
    {
        TargetExists = targetExists;
        WriteCount = writeCount;
        LatestSourcePosition = latestSourcePosition;
        LatestPayload = latestPayload;
    }

    /// <summary>
    ///     Gets the latest applied payload.
    /// </summary>
    public object? LatestPayload { get; }

    /// <summary>
    ///     Gets the latest applied source position.
    /// </summary>
    public long? LatestSourcePosition { get; }

    /// <summary>
    ///     Gets a value indicating whether the target marker exists.
    /// </summary>
    public bool TargetExists { get; }

    /// <summary>
    ///     Gets the total number of applied writes observed for the target.
    /// </summary>
    public long WriteCount { get; }
}