namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.L2Tests;

/// <summary>
///     Captures the scenario-local metrics emitted by one deterministic calibration run.
/// </summary>
internal sealed class CosmosReplicaSinkCalibrationScenarioMetrics
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosReplicaSinkCalibrationScenarioMetrics" /> class.
    /// </summary>
    /// <param name="scenarioName">The stable scenario name.</param>
    /// <param name="entityCount">The deterministic entity count.</param>
    /// <param name="sinkCount">The number of sink registrations participating in the scenario.</param>
    /// <param name="payloadSizeClass">The payload envelope size class.</param>
    /// <param name="writeCount">The total provider write count executed across all sinks.</param>
    /// <param name="inFlightBudget">The bounded in-flight concurrency budget.</param>
    /// <param name="replayElapsedMilliseconds">The elapsed replay/backlog-drain time in milliseconds.</param>
    /// <param name="liveWriteElapsedMilliseconds">The elapsed live-write time in milliseconds, when applicable.</param>
    /// <param name="retryCount">The count of due retries observed after the run.</param>
    /// <param name="deadLetterCount">The count of dead-letter entries observed after the run.</param>
    /// <param name="targets">The per-target inspection metrics captured after the run.</param>
    public CosmosReplicaSinkCalibrationScenarioMetrics(
        string scenarioName,
        int entityCount,
        int sinkCount,
        string payloadSizeClass,
        int writeCount,
        int inFlightBudget,
        double replayElapsedMilliseconds,
        double? liveWriteElapsedMilliseconds,
        int retryCount,
        int deadLetterCount,
        IReadOnlyList<CosmosReplicaSinkCalibrationTargetMetrics> targets
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadSizeClass);
        ArgumentOutOfRangeException.ThrowIfLessThan(entityCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(sinkCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(writeCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(inFlightBudget, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(replayElapsedMilliseconds, 0D);
        ArgumentOutOfRangeException.ThrowIfNegative(retryCount);
        ArgumentOutOfRangeException.ThrowIfNegative(deadLetterCount);
        ArgumentNullException.ThrowIfNull(targets);
        ScenarioName = scenarioName;
        EntityCount = entityCount;
        SinkCount = sinkCount;
        PayloadSizeClass = payloadSizeClass;
        WriteCount = writeCount;
        InFlightBudget = inFlightBudget;
        ReplayElapsedMilliseconds = replayElapsedMilliseconds;
        LiveWriteElapsedMilliseconds = liveWriteElapsedMilliseconds;
        RetryCount = retryCount;
        DeadLetterCount = deadLetterCount;
        Targets = targets;
    }

    /// <summary>
    ///     Gets the count of dead-letter entries observed after the run.
    /// </summary>
    public int DeadLetterCount { get; }

    /// <summary>
    ///     Gets the deterministic entity count.
    /// </summary>
    public int EntityCount { get; }

    /// <summary>
    ///     Gets the bounded in-flight concurrency budget.
    /// </summary>
    public int InFlightBudget { get; }

    /// <summary>
    ///     Gets the elapsed live-write time in milliseconds, when applicable.
    /// </summary>
    public double? LiveWriteElapsedMilliseconds { get; }

    /// <summary>
    ///     Gets the payload envelope size class.
    /// </summary>
    public string PayloadSizeClass { get; }

    /// <summary>
    ///     Gets the elapsed replay/backlog-drain time in milliseconds.
    /// </summary>
    public double ReplayElapsedMilliseconds { get; }

    /// <summary>
    ///     Gets the count of due retries observed after the run.
    /// </summary>
    public int RetryCount { get; }

    /// <summary>
    ///     Gets the stable scenario name.
    /// </summary>
    public string ScenarioName { get; }

    /// <summary>
    ///     Gets the number of sink registrations participating in the scenario.
    /// </summary>
    public int SinkCount { get; }

    /// <summary>
    ///     Gets the per-target inspection metrics captured after the run.
    /// </summary>
    public IReadOnlyList<CosmosReplicaSinkCalibrationTargetMetrics> Targets { get; }

    /// <summary>
    ///     Gets the total provider write count executed across all sinks.
    /// </summary>
    public int WriteCount { get; }
}
