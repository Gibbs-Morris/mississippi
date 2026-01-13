using System;
using System.Collections.Generic;
using System.Globalization;

using Microsoft.AspNetCore.Components;


namespace Cascade.Client.Components.Atoms.Timestamp;

/// <summary>
///     Atomic component for displaying formatted date and time values.
/// </summary>
public sealed partial class Timestamp : ComponentBase
{
    /// <summary>
    ///     Gets or sets any additional HTML attributes to apply to the element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    ///     Gets or sets the format string. Used when ShowRelative is false.
    /// </summary>
    [Parameter]
    public string Format { get; set; } = "HH:mm";

    /// <summary>
    ///     Gets or sets a value indicating whether to show relative time (e.g., "5 min ago").
    /// </summary>
    [Parameter]
    public bool ShowRelative { get; set; }

    /// <summary>
    ///     Gets or sets the date/time value to display.
    /// </summary>
    [Parameter]
    public DateTimeOffset Value { get; set; }

    private DateTimeOffset DateTimeValue => Value;

    private string FormattedTime =>
        ShowRelative ? GetRelativeTime(Value) : Value.ToString(Format, CultureInfo.InvariantCulture);

    private string FullDateTime => Value.ToString("f", CultureInfo.InvariantCulture);

    private static string GetRelativeTime(
        DateTimeOffset dateTime
    )
    {
        TimeSpan diff = DateTimeOffset.UtcNow - dateTime;
        return diff.TotalSeconds switch
        {
            < 60 => "just now",
            < 3600 => $"{(int)diff.TotalMinutes}m ago",
            < 86400 => $"{(int)diff.TotalHours}h ago",
            < 604800 => $"{(int)diff.TotalDays}d ago",
            var _ => dateTime.ToString("MMM d", CultureInfo.InvariantCulture),
        };
    }
}