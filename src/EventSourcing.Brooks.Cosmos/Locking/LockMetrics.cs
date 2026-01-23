using System.Diagnostics;
using System.Diagnostics.Metrics;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Locking;

/// <summary>
///     OpenTelemetry metrics for distributed lock operations.
/// </summary>
internal static class LockMetrics
{
    /// <summary>
    ///     The meter name for lock metrics.
    /// </summary>
    internal const string MeterName = "Mississippi.Storage.Locking";

    private const string LockKeyTag = "lock.key";

    private static readonly Meter LockMeter = new(MeterName);

    private static readonly Counter<long> AcquireCount = LockMeter.CreateCounter<long>(
        "lock.acquire.count",
        "acquisitions",
        "Number of lock acquisition attempts.");

    private static readonly Histogram<double> AcquireDuration = LockMeter.CreateHistogram<double>(
        "lock.acquire.duration",
        "ms",
        "Time to acquire a distributed lock.");

    private static readonly Counter<long> AcquireFailures = LockMeter.CreateCounter<long>(
        "lock.acquire.failures",
        "failures",
        "Number of lock acquisition failures after all retries.");

    private static readonly Counter<long> ContentionWaits = LockMeter.CreateCounter<long>(
        "lock.contention.waits",
        "waits",
        "Number of 409 Conflict responses during lock acquisition.");

    private static readonly Histogram<double> HeldDuration = LockMeter.CreateHistogram<double>(
        "lock.held.duration",
        "ms",
        "Time a lock is held before release.");

    private static readonly Histogram<int> RetryAttempts = LockMeter.CreateHistogram<int>(
        "lock.acquire.attempts",
        "attempts",
        "Number of retry attempts per acquisition.");

    /// <summary>
    ///     Record a failed lock acquisition.
    /// </summary>
    /// <param name="lockKey">The lock key.</param>
    /// <param name="durationMs">The time spent attempting to acquire the lock in milliseconds.</param>
    /// <param name="attempts">The number of attempts made.</param>
    internal static void RecordAcquireFailure(
        string lockKey,
        double durationMs,
        int attempts
    )
    {
        TagList tags = default;
        tags.Add(LockKeyTag, SanitizeLockKey(lockKey));
        tags.Add("result", "failure");
        AcquireCount.Add(1, tags);
        AcquireDuration.Record(durationMs, tags);
        RetryAttempts.Record(attempts, tags);
        AcquireFailures.Add(1, tags);
    }

    /// <summary>
    ///     Record a successful lock acquisition.
    /// </summary>
    /// <param name="lockKey">The lock key.</param>
    /// <param name="durationMs">The time taken to acquire the lock in milliseconds.</param>
    /// <param name="attempts">The number of attempts required.</param>
    internal static void RecordAcquireSuccess(
        string lockKey,
        double durationMs,
        int attempts
    )
    {
        TagList tags = default;
        tags.Add(LockKeyTag, SanitizeLockKey(lockKey));
        tags.Add("result", "success");
        AcquireCount.Add(1, tags);
        AcquireDuration.Record(durationMs, tags);
        RetryAttempts.Record(attempts, tags);
    }

    /// <summary>
    ///     Record a contention wait (409 Conflict).
    /// </summary>
    /// <param name="lockKey">The lock key.</param>
    internal static void RecordContentionWait(
        string lockKey
    )
    {
        TagList tags = default;
        tags.Add(LockKeyTag, SanitizeLockKey(lockKey));
        ContentionWaits.Add(1, tags);
    }

    /// <summary>
    ///     Record the duration a lock was held.
    /// </summary>
    /// <param name="lockKey">The lock key.</param>
    /// <param name="durationMs">The time the lock was held in milliseconds.</param>
    internal static void RecordHeldDuration(
        string lockKey,
        double durationMs
    )
    {
        TagList tags = default;
        tags.Add(LockKeyTag, SanitizeLockKey(lockKey));
        HeldDuration.Record(durationMs, tags);
    }

    /// <summary>
    ///     Sanitizes lock key for use as a metric tag (extracts brook name).
    /// </summary>
    private static string SanitizeLockKey(
        string lockKey
    )
    {
        // Lock keys are typically brook keys like "MYAPP|CHAT|CONVERSATION|demo-conversation"
        // Extract just the brook name portion for cardinality control
        int lastPipeIndex = lockKey.LastIndexOf('|');
        return lastPipeIndex > 0 ? lockKey[..lastPipeIndex] : lockKey;
    }
}