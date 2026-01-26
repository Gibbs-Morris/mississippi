using System.Diagnostics;
using System.Diagnostics.Metrics;


namespace Mississippi.Inlet.Silo.Diagnostics;

/// <summary>
///     OpenTelemetry metrics for Inlet real-time notifications.
/// </summary>
internal static class InletMetrics
{
    /// <summary>
    ///     The meter name for Inlet metrics.
    /// </summary>
    internal const string MeterName = "Mississippi.Inlet";

    private static readonly Meter InletMeter = new(MeterName);

    private static readonly Counter<long> CursorEventReceived = InletMeter.CreateCounter<long>(
        "inlet.cursor.event.received",
        "events",
        "Number of cursor events received.");

    // Brook stream metrics
    private static readonly Histogram<double> NotificationDuration = InletMeter.CreateHistogram<double>(
        "inlet.notification.duration",
        "ms",
        "Time to deliver notification.");

    private static readonly Counter<long> NotificationErrors = InletMeter.CreateCounter<long>(
        "inlet.notification.errors",
        "errors",
        "Number of notification delivery failures.");

    // Notification metrics
    private static readonly Counter<long> NotificationSent = InletMeter.CreateCounter<long>(
        "inlet.notification.sent",
        "notifications",
        "Number of notifications sent to clients.");

    // Subscription metrics
    private static readonly Counter<long> SubscriptionCount = InletMeter.CreateCounter<long>(
        "inlet.subscription.count",
        "subscriptions",
        "Number of subscriptions created or removed.");

    /// <summary>
    ///     Record a cursor event received.
    /// </summary>
    /// <param name="brookName">The brook name.</param>
    internal static void RecordCursorEventReceived(
        string brookName
    )
    {
        TagList tags = default;
        tags.Add("brook.name", brookName);
        CursorEventReceived.Add(1, tags);
    }

    /// <summary>
    ///     Record a notification error.
    /// </summary>
    /// <param name="projectionPath">The projection API path.</param>
    /// <param name="errorType">The error type.</param>
    internal static void RecordNotificationError(
        string projectionPath,
        string errorType
    )
    {
        TagList tags = default;
        tags.Add("projection.path", projectionPath);
        tags.Add("error.type", errorType);
        NotificationErrors.Add(1, tags);
    }

    /// <summary>
    ///     Record a successful notification.
    /// </summary>
    /// <param name="projectionPath">The projection API path.</param>
    /// <param name="durationMs">The duration of the notification in milliseconds.</param>
    internal static void RecordNotificationSent(
        string projectionPath,
        double durationMs
    )
    {
        TagList tags = default;
        tags.Add("projection.path", projectionPath);
        NotificationSent.Add(1, tags);
        NotificationDuration.Record(durationMs, tags);
    }

    /// <summary>
    ///     Record a subscription event.
    /// </summary>
    /// <param name="projectionPath">The projection API path.</param>
    /// <param name="action">The action (subscribe or unsubscribe).</param>
    internal static void RecordSubscription(
        string projectionPath,
        string action
    )
    {
        TagList tags = default;
        tags.Add("projection.path", projectionPath);
        tags.Add("action", action);
        SubscriptionCount.Add(1, tags);
    }
}