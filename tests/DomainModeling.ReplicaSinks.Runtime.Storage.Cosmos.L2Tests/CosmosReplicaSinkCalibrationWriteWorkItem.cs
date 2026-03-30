namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L2Tests;

/// <summary>
///     Represents one deterministic provider write scheduled by the calibration runner.
/// </summary>
internal sealed class CosmosReplicaSinkCalibrationWriteWorkItem
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosReplicaSinkCalibrationWriteWorkItem" /> class.
    /// </summary>
    /// <param name="sinkKey">The sink key receiving the write.</param>
    /// <param name="target">The target descriptor receiving the write.</param>
    /// <param name="deliveryKey">The runtime delivery key associated with the write.</param>
    /// <param name="sourcePosition">The source position represented by the write.</param>
    /// <param name="payload">The deterministic payload body.</param>
    public CosmosReplicaSinkCalibrationWriteWorkItem(
        string sinkKey,
        ReplicaTargetDescriptor target,
        string deliveryKey,
        long sourcePosition,
        CosmosReplicaSinkCalibrationPayload payload
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryKey);
        ArgumentOutOfRangeException.ThrowIfNegative(sourcePosition);
        ArgumentNullException.ThrowIfNull(payload);
        SinkKey = sinkKey;
        Target = target;
        DeliveryKey = deliveryKey;
        SourcePosition = sourcePosition;
        Payload = payload;
    }

    /// <summary>
    ///     Gets the runtime delivery key associated with the write.
    /// </summary>
    public string DeliveryKey { get; }

    /// <summary>
    ///     Gets the deterministic payload body.
    /// </summary>
    public CosmosReplicaSinkCalibrationPayload Payload { get; }

    /// <summary>
    ///     Gets the sink key receiving the write.
    /// </summary>
    public string SinkKey { get; }

    /// <summary>
    ///     Gets the source position represented by the write.
    /// </summary>
    public long SourcePosition { get; }

    /// <summary>
    ///     Gets the target descriptor receiving the write.
    /// </summary>
    public ReplicaTargetDescriptor Target { get; }
}
