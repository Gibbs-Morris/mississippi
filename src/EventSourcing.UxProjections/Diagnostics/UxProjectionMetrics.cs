using System.Diagnostics;
using System.Diagnostics.Metrics;


namespace Mississippi.EventSourcing.UxProjections.Diagnostics;

/// <summary>
///     OpenTelemetry metrics for UX projection queries and subscriptions.
/// </summary>
internal static class UxProjectionMetrics
{
    /// <summary>
    ///     The meter name for UX projection metrics.
    /// </summary>
    internal const string MeterName = "Mississippi.EventSourcing.UxProjections";

    private const string ProjectionTypeTag = "projection.type";

    private static readonly Meter ProjectionMeter = new(MeterName);

    private static readonly Counter<long> CursorReads = ProjectionMeter.CreateCounter<long>(
        "projection.cursor.reads",
        "reads",
        "Number of cursor position reads.");

    private static readonly Counter<long> NotificationSent = ProjectionMeter.CreateCounter<long>(
        "projection.notification.sent",
        "notifications",
        "Number of change notifications sent.");

    // Cursor metrics

    // Query metrics
    private static readonly Counter<long> QueryCount = ProjectionMeter.CreateCounter<long>(
        "projection.query.count",
        "queries",
        "Number of projection queries.");

    private static readonly Histogram<double> QueryDuration = ProjectionMeter.CreateHistogram<double>(
        "projection.query.duration",
        "ms",
        "Time to query projection.");

    private static readonly Counter<long> QueryEmpty = ProjectionMeter.CreateCounter<long>(
        "projection.query.empty",
        "queries",
        "Number of queries returning no data.");

    // Subscription metrics
    private static readonly Counter<long> SubscriptionCount = ProjectionMeter.CreateCounter<long>(
        "projection.subscription.count",
        "subscriptions",
        "Number of subscriptions created or removed.");

    // Cache metrics
    private static readonly Counter<long> VersionCacheHits = ProjectionMeter.CreateCounter<long>(
        "projection.version.cache.hits",
        "hits",
        "Number of versioned cache hits.");

    /// <summary>
    ///     Record a cursor position read.
    /// </summary>
    /// <param name="projectionType">The projection type name.</param>
    internal static void RecordCursorRead(
        string projectionType
    )
    {
        TagList tags = default;
        tags.Add(ProjectionTypeTag, projectionType);
        CursorReads.Add(1, tags);
    }

    /// <summary>
    ///     Record a notification sent.
    /// </summary>
    /// <param name="projectionType">The projection type name.</param>
    internal static void RecordNotificationSent(
        string projectionType
    )
    {
        TagList tags = default;
        tags.Add(ProjectionTypeTag, projectionType);
        NotificationSent.Add(1, tags);
    }

    /// <summary>
    ///     Record a projection query.
    /// </summary>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="queryType">The query type (latest or versioned).</param>
    /// <param name="durationMs">The duration of the query in milliseconds.</param>
    /// <param name="hasResult">Whether the query returned data.</param>
    internal static void RecordQuery(
        string projectionType,
        string queryType,
        double durationMs,
        bool hasResult
    )
    {
        TagList tags = default;
        tags.Add(ProjectionTypeTag, projectionType);
        tags.Add("query.type", queryType);
        QueryCount.Add(1, tags);
        QueryDuration.Record(durationMs, tags);
        if (!hasResult)
        {
            TagList emptyTags = default;
            emptyTags.Add(ProjectionTypeTag, projectionType);
            QueryEmpty.Add(1, emptyTags);
        }
    }

    /// <summary>
    ///     Record a subscription event.
    /// </summary>
    /// <param name="action">The action (subscribe or unsubscribe).</param>
    internal static void RecordSubscription(
        string action
    )
    {
        TagList tags = default;
        tags.Add("action", action);
        SubscriptionCount.Add(1, tags);
    }

    /// <summary>
    ///     Record a versioned cache hit.
    /// </summary>
    /// <param name="projectionType">The projection type name.</param>
    internal static void RecordVersionCacheHit(
        string projectionType
    )
    {
        TagList tags = default;
        tags.Add(ProjectionTypeTag, projectionType);
        VersionCacheHits.Add(1, tags);
    }
}