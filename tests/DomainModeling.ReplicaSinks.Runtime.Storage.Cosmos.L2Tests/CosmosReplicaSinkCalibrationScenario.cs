namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L2Tests;

/// <summary>
///     Describes one deterministic provider-calibration scenario.
/// </summary>
internal sealed class CosmosReplicaSinkCalibrationScenario
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosReplicaSinkCalibrationScenario" /> class.
    /// </summary>
    /// <param name="name">The stable scenario name.</param>
    /// <param name="entityCount">The deterministic entity count.</param>
    /// <param name="sinkCount">The number of sink registrations participating in the scenario.</param>
    /// <param name="liveWriteCount">The number of live writes appended after replay, when applicable.</param>
    /// <param name="payloadSizeClass">The payload envelope size class.</param>
    /// <param name="inFlightBudget">The bounded in-flight concurrency budget.</param>
    /// <param name="seed">The deterministic seed used for payload shaping.</param>
    public CosmosReplicaSinkCalibrationScenario(
        string name,
        int entityCount,
        int sinkCount,
        int liveWriteCount,
        CosmosReplicaSinkCalibrationPayloadSizeClass payloadSizeClass,
        int inFlightBudget,
        int seed
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(entityCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(sinkCount, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(liveWriteCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(inFlightBudget, 1);
        if (liveWriteCount > entityCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(liveWriteCount),
                "Live-write count cannot exceed the deterministic entity count.");
        }

        Name = name;
        EntityCount = entityCount;
        SinkCount = sinkCount;
        LiveWriteCount = liveWriteCount;
        PayloadSizeClass = payloadSizeClass;
        InFlightBudget = inFlightBudget;
        Seed = seed;
    }

    /// <summary>
    ///     Gets the deterministic entity count.
    /// </summary>
    public int EntityCount { get; }

    /// <summary>
    ///     Gets the bounded in-flight concurrency budget.
    /// </summary>
    public int InFlightBudget { get; }

    /// <summary>
    ///     Gets the number of live writes appended after replay, when applicable.
    /// </summary>
    public int LiveWriteCount { get; }

    /// <summary>
    ///     Gets the stable scenario name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the payload envelope size class.
    /// </summary>
    public CosmosReplicaSinkCalibrationPayloadSizeClass PayloadSizeClass { get; }

    /// <summary>
    ///     Gets the approximate payload size emitted by each deterministic write.
    /// </summary>
    public int PayloadBytes => PayloadSizeClass == CosmosReplicaSinkCalibrationPayloadSizeClass.Small ? 256 : 2_048;

    /// <summary>
    ///     Gets the deterministic seed used for payload shaping.
    /// </summary>
    public int Seed { get; }

    /// <summary>
    ///     Gets the number of sink registrations participating in the scenario.
    /// </summary>
    public int SinkCount { get; }

    /// <summary>
    ///     Gets the total provider write count executed across all sinks.
    /// </summary>
    public int TotalWriteCount => (EntityCount + LiveWriteCount) * SinkCount;

    /// <summary>
    ///     Creates the replay-backlog-followed-by-live-writes scenario.
    /// </summary>
    /// <returns>The deterministic replay-plus-live scenario.</returns>
    public static CosmosReplicaSinkCalibrationScenario CreateReplayBacklogFollowedByLiveWrites() =>
        new(
            "replay-backlog-followed-by-live-writes",
            entityCount: 18,
            sinkCount: 1,
            liveWriteCount: 6,
            payloadSizeClass: CosmosReplicaSinkCalibrationPayloadSizeClass.Medium,
            inFlightBudget: 4,
            seed: 17);

    /// <summary>
    ///     Creates the single-sink replay-backlog scenario.
    /// </summary>
    /// <returns>The deterministic single-sink replay scenario.</returns>
    public static CosmosReplicaSinkCalibrationScenario CreateSingleSinkReplayBacklog() =>
        new(
            "single-sink-replay-backlog",
            entityCount: 18,
            sinkCount: 1,
            liveWriteCount: 0,
            payloadSizeClass: CosmosReplicaSinkCalibrationPayloadSizeClass.Small,
            inFlightBudget: 4,
            seed: 11);

    /// <summary>
    ///     Creates the two-sink fan-out replay-backlog scenario.
    /// </summary>
    /// <returns>The deterministic two-sink fan-out replay scenario.</returns>
    public static CosmosReplicaSinkCalibrationScenario CreateTwoSinkFanOutReplayBacklog() =>
        new(
            "two-sink-fan-out-replay-backlog",
            entityCount: 18,
            sinkCount: 2,
            liveWriteCount: 0,
            payloadSizeClass: CosmosReplicaSinkCalibrationPayloadSizeClass.Medium,
            inFlightBudget: 6,
            seed: 23);
}
