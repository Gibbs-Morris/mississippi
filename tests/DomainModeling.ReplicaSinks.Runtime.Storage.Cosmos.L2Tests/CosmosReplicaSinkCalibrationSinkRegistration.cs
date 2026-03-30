namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L2Tests;

/// <summary>
///     Describes one named sink registration participating in a calibration run.
/// </summary>
internal sealed class CosmosReplicaSinkCalibrationSinkRegistration
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosReplicaSinkCalibrationSinkRegistration" /> class.
    /// </summary>
    /// <param name="sinkKey">The named sink registration key.</param>
    /// <param name="containerId">The Cosmos container identifier used by the sink.</param>
    /// <param name="target">The target descriptor used by the sink.</param>
    public CosmosReplicaSinkCalibrationSinkRegistration(
        string sinkKey,
        string containerId,
        ReplicaTargetDescriptor target
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);
        ArgumentNullException.ThrowIfNull(target);
        SinkKey = sinkKey;
        ContainerId = containerId;
        Target = target;
    }

    /// <summary>
    ///     Gets the Cosmos container identifier used by the sink.
    /// </summary>
    public string ContainerId { get; }

    /// <summary>
    ///     Gets the named sink registration key.
    /// </summary>
    public string SinkKey { get; }

    /// <summary>
    ///     Gets the target descriptor used by the sink.
    /// </summary>
    public ReplicaTargetDescriptor Target { get; }
}
