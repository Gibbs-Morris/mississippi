namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L2Tests;

/// <summary>
///     Represents the deterministic payload body written by one calibration operation.
/// </summary>
internal sealed class CosmosReplicaSinkCalibrationPayload
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosReplicaSinkCalibrationPayload" /> class.
    /// </summary>
    /// <param name="entityId">The stable entity identifier.</param>
    /// <param name="sourcePosition">The source position represented by the payload.</param>
    /// <param name="payloadBytes">The approximate payload size.</param>
    /// <param name="isLivePhase">A value indicating whether the payload belongs to the live-write phase.</param>
    /// <param name="data">The serialized payload body.</param>
    public CosmosReplicaSinkCalibrationPayload(
        string entityId,
        long sourcePosition,
        int payloadBytes,
        bool isLivePhase,
        string data
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentOutOfRangeException.ThrowIfNegative(sourcePosition);
        ArgumentOutOfRangeException.ThrowIfLessThan(payloadBytes, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(data);
        EntityId = entityId;
        SourcePosition = sourcePosition;
        PayloadBytes = payloadBytes;
        IsLivePhase = isLivePhase;
        Data = data;
    }

    /// <summary>
    ///     Gets the serialized payload body.
    /// </summary>
    public string Data { get; }

    /// <summary>
    ///     Gets the stable entity identifier.
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    ///     Gets a value indicating whether the payload belongs to the live-write phase.
    /// </summary>
    public bool IsLivePhase { get; }

    /// <summary>
    ///     Gets the approximate payload size.
    /// </summary>
    public int PayloadBytes { get; }

    /// <summary>
    ///     Gets the source position represented by the payload.
    /// </summary>
    public long SourcePosition { get; }
}
