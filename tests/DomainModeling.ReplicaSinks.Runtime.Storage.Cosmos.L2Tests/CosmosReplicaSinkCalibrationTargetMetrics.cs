namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L2Tests;

/// <summary>
///     Captures the post-run inspection metrics for one sink target.
/// </summary>
internal sealed class CosmosReplicaSinkCalibrationTargetMetrics
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosReplicaSinkCalibrationTargetMetrics" /> class.
    /// </summary>
    /// <param name="sinkKey">The sink key owning the inspected target.</param>
    /// <param name="containerId">The Cosmos container identifier used by the sink.</param>
    /// <param name="targetName">The provider-neutral target name.</param>
    /// <param name="targetExists">A value indicating whether the target exists after the run.</param>
    /// <param name="writeCount">The number of writes observed by the provider for the target.</param>
    /// <param name="latestSourcePosition">The latest applied source position.</param>
    /// <param name="sampleCommittedSourcePosition">The committed source position read back from the durable state store.</param>
    public CosmosReplicaSinkCalibrationTargetMetrics(
        string sinkKey,
        string containerId,
        string targetName,
        bool targetExists,
        long writeCount,
        long? latestSourcePosition,
        long? sampleCommittedSourcePosition
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetName);
        ArgumentOutOfRangeException.ThrowIfNegative(writeCount);
        SinkKey = sinkKey;
        ContainerId = containerId;
        TargetName = targetName;
        TargetExists = targetExists;
        WriteCount = writeCount;
        LatestSourcePosition = latestSourcePosition;
        SampleCommittedSourcePosition = sampleCommittedSourcePosition;
    }

    /// <summary>
    ///     Gets the Cosmos container identifier used by the sink.
    /// </summary>
    public string ContainerId { get; }

    /// <summary>
    ///     Gets the latest applied source position.
    /// </summary>
    public long? LatestSourcePosition { get; }

    /// <summary>
    ///     Gets the committed source position read back from the durable state store.
    /// </summary>
    public long? SampleCommittedSourcePosition { get; }

    /// <summary>
    ///     Gets the sink key owning the inspected target.
    /// </summary>
    public string SinkKey { get; }

    /// <summary>
    ///     Gets the provider-neutral target name.
    /// </summary>
    public string TargetName { get; }

    /// <summary>
    ///     Gets a value indicating whether the target exists after the run.
    /// </summary>
    public bool TargetExists { get; }

    /// <summary>
    ///     Gets the number of writes observed by the provider for the target.
    /// </summary>
    public long WriteCount { get; }
}
