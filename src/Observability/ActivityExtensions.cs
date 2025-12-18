using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;


namespace Mississippi.Observability;

/// <summary>
///     Extension methods for working with <see cref="Activity" /> in a null-safe and low-allocation manner.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    ///     Sets a tag on the activity if the activity exists and has data requested.
    /// </summary>
    /// <param name="activity">The activity to set the tag on.</param>
    /// <param name="key">The tag key.</param>
    /// <param name="value">The tag value.</param>
    /// <returns>The activity for chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? SetTagSafe(
        this Activity? activity,
        string key,
        object? value
    )
    {
        if (activity?.IsAllDataRequested == true)
        {
            activity.SetTag(key, value);
        }

        return activity;
    }

    /// <summary>
    ///     Records an exception on the activity and sets error status if the activity exists.
    ///     Uses standard Activity event recording for OTel-compatible exception events.
    /// </summary>
    /// <param name="activity">The activity to record the exception on.</param>
    /// <param name="exception">The exception to record.</param>
    /// <returns>The activity for chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? RecordExceptionSafe(
        this Activity? activity,
        Exception exception
    )
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (activity is null)
        {
            return null;
        }

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);

        // Record exception as an Activity event following OTel semantic conventions
        // This is compatible with OpenTelemetry but doesn't require OTel packages
        ActivityTagsCollection tags = new()
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.ToString() },
        };
        activity.AddEvent(new ActivityEvent("exception", tags: tags));

        return activity;
    }

    /// <summary>
    ///     Sets the status to error on the activity if the activity exists.
    /// </summary>
    /// <param name="activity">The activity to set the status on.</param>
    /// <param name="description">The error description.</param>
    /// <returns>The activity for chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? SetErrorStatus(
        this Activity? activity,
        string? description = null
    )
    {
        activity?.SetStatus(ActivityStatusCode.Error, description);
        return activity;
    }

    /// <summary>
    ///     Sets the status to OK on the activity if the activity exists.
    /// </summary>
    /// <param name="activity">The activity to set the status on.</param>
    /// <returns>The activity for chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? SetSuccessStatus(
        this Activity? activity
    )
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
        return activity;
    }
}
