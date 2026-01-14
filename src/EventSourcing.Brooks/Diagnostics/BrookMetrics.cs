using System.Diagnostics;
using System.Diagnostics.Metrics;

using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.Brooks.Diagnostics;

/// <summary>
///     OpenTelemetry metrics for brook read/write operations.
/// </summary>
internal static class BrookMetrics
{
    /// <summary>
    ///     The meter name for brook metrics.
    /// </summary>
    internal const string MeterName = "Mississippi.EventSourcing.Brooks";

    private const string BrookNameTag = "brook.name";

    private static readonly Meter BrookMeter = new(MeterName);

    // Cursor metrics
    private static readonly Counter<long> CursorReads = BrookMeter.CreateCounter<long>(
        "brook.cursor.reads",
        "reads",
        "Number of cursor position reads.");

    private static readonly Histogram<double> ReaderDuration = BrookMeter.CreateHistogram<double>(
        "brook.reader.duration",
        "ms",
        "Time to read events from a brook.");

    // Reader metrics
    private static readonly Counter<long> ReaderEvents = BrookMeter.CreateCounter<long>(
        "brook.reader.events",
        "events",
        "Number of events read from a brook.");

    private static readonly Counter<long> ReaderSlices = BrookMeter.CreateCounter<long>(
        "brook.reader.slices",
        "slices",
        "Number of slice reader fan-outs.");

    private static readonly Histogram<int> WriterBatchSize = BrookMeter.CreateHistogram<int>(
        "brook.writer.batch.size",
        "events",
        "Number of events per append batch.");

    private static readonly Histogram<double> WriterDuration = BrookMeter.CreateHistogram<double>(
        "brook.writer.duration",
        "ms",
        "Time to append events to a brook.");

    private static readonly Counter<long> WriterErrors = BrookMeter.CreateCounter<long>(
        "brook.writer.errors",
        "errors",
        "Number of append failures.");

    // Writer metrics
    private static readonly Counter<long> WriterEvents = BrookMeter.CreateCounter<long>(
        "brook.writer.events",
        "events",
        "Number of events appended to a brook.");

    /// <summary>
    ///     Record a cursor position read.
    /// </summary>
    /// <param name="brookKey">The brook key being read.</param>
    /// <param name="readType">The type of read (cached or confirmed).</param>
    internal static void RecordCursorRead(
        BrookKey brookKey,
        string readType
    )
    {
        TagList tags = default;
        tags.Add(BrookNameTag, brookKey.BrookName);
        tags.Add("read.type", readType);
        CursorReads.Add(1, tags);
    }

    /// <summary>
    ///     Record a successful read operation.
    /// </summary>
    /// <param name="brookKey">The brook key being read from.</param>
    /// <param name="eventCount">The number of events read.</param>
    /// <param name="durationMs">The duration of the read operation in milliseconds.</param>
    /// <param name="readMode">The read mode (batch or stream).</param>
    internal static void RecordRead(
        BrookKey brookKey,
        int eventCount,
        double durationMs,
        string readMode
    )
    {
        if (eventCount <= 0)
        {
            return;
        }

        TagList tags = default;
        tags.Add(BrookNameTag, brookKey.BrookName);
        tags.Add("read.mode", readMode);
        ReaderEvents.Add(eventCount, tags);
        ReaderDuration.Record(durationMs, tags);
    }

    /// <summary>
    ///     Record slice reader fan-out.
    /// </summary>
    /// <param name="brookKey">The brook key being read from.</param>
    /// <param name="sliceCount">The number of slices used.</param>
    internal static void RecordSliceFanOut(
        BrookKey brookKey,
        int sliceCount
    )
    {
        if (sliceCount <= 0)
        {
            return;
        }

        TagList tags = default;
        tags.Add(BrookNameTag, brookKey.BrookName);
        ReaderSlices.Add(sliceCount, tags);
    }

    /// <summary>
    ///     Record a successful write operation.
    /// </summary>
    /// <param name="brookKey">The brook key being written to.</param>
    /// <param name="eventCount">The number of events appended.</param>
    /// <param name="durationMs">The duration of the write operation in milliseconds.</param>
    internal static void RecordWrite(
        BrookKey brookKey,
        int eventCount,
        double durationMs
    )
    {
        if (eventCount <= 0)
        {
            return;
        }

        TagList tags = default;
        tags.Add(BrookNameTag, brookKey.BrookName);
        WriterEvents.Add(eventCount, tags);
        WriterDuration.Record(durationMs, tags);
        WriterBatchSize.Record(eventCount, tags);
    }

    /// <summary>
    ///     Record a write error.
    /// </summary>
    /// <param name="brookKey">The brook key being written to.</param>
    /// <param name="errorType">The type of error that occurred.</param>
    internal static void RecordWriteError(
        BrookKey brookKey,
        string errorType
    )
    {
        TagList tags = default;
        tags.Add(BrookNameTag, brookKey.BrookName);
        tags.Add("error.type", errorType);
        WriterErrors.Add(1, tags);
    }
}