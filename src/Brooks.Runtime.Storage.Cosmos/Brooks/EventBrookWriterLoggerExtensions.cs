using System;

using Microsoft.Extensions.Logging;

using Mississippi.Brooks.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.Brooks;

/// <summary>
///     Structured diagnostic events for brook writes.
/// </summary>
internal static partial class EventBrookWriterLoggerExtensions
{
    /// <summary>
    ///     Reports shutdown failure after the append's cursor commit has been acknowledged.
    /// </summary>
    /// <param name="logger">The writer logger.</param>
    /// <param name="exception">The first shutdown failure.</param>
    /// <param name="brookId">The brook whose events are committed.</param>
    /// <param name="finalPosition">The acknowledged cursor position.</param>
    [LoggerMessage(
        EventId = 1014,
        EventName = "AppendCleanupFailed",
        Level = LogLevel.Warning,
        Message =
            "Append cleanup failed for brook '{BrookId}' after committing position {FinalPosition}; the committed result was returned")]
    public static partial void AppendCleanupFailed(
        this ILogger logger,
        Exception exception,
        BrookKey brookId,
        long finalPosition
    );

    /// <summary>
    ///     Logs an uncertain cursor commit without implying that event rollback is safe.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The original commit failure.</param>
    /// <param name="brookId">The brook being appended to.</param>
    /// <param name="finalPosition">The attempted cursor position.</param>
    [LoggerMessage(
        EventId = 1013,
        Level = LogLevel.Error,
        Message =
            "Cursor commit failed for brook '{BrookId}' at position {FinalPosition}; appended events were retained")]
    public static partial void CursorCommitFailed(
        this ILogger logger,
        Exception exception,
        BrookKey brookId,
        long finalPosition
    );
}