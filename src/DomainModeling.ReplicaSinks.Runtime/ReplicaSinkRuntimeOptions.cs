using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Defines bounded execution, retry, and operator paging limits for the replica sink runtime.
/// </summary>
public sealed class ReplicaSinkRuntimeOptions
{
    /// <summary>
    ///     Gets or sets the authentication-failure park duration applied to unhealthy sinks.
    /// </summary>
    public TimeSpan AuthFailureParkDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Gets or sets the periodic execution-poll interval for the runtime pump.
    /// </summary>
    public TimeSpan ExecutionPollInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     Gets or sets the maximum dead-letter page size exposed by the runtime operator surface.
    /// </summary>
    public int MaxDeadLetterPageSize { get; set; } = 50;

    /// <summary>
    ///     Gets or sets the maximum number of projection/entity work items processed during a single batch.
    /// </summary>
    public int MaxExecutionBatchSize { get; set; } = 8;

    /// <summary>
    ///     Gets or sets the maximum number of work items for a single sink processed during a single batch.
    /// </summary>
    public int MaxExecutionBatchSizePerSink { get; set; } = 4;

    /// <summary>
    ///     Gets or sets the maximum number of due retry snapshots hydrated during a single batch.
    /// </summary>
    public int MaxRetrySelectionsPerBatch { get; set; } = 16;

    /// <summary>
    ///     Gets or sets the maximum number of due retry snapshots selected per sink during a single batch.
    /// </summary>
    public int MaxRetrySelectionsPerSink { get; set; } = 4;

    /// <summary>
    ///     Gets or sets the provider-failure throttle duration applied after repeated retryable provider failures.
    /// </summary>
    public TimeSpan RepeatedProviderFailureThrottleDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     Gets or sets the retry-attempt threshold that triggers provider-failure throttling.
    /// </summary>
    public int RepeatedProviderFailureThrottleThreshold { get; set; } = 3;
}
