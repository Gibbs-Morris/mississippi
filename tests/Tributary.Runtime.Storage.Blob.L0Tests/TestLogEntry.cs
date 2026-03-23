using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Represents a captured log entry for test assertions.
/// </summary>
internal sealed class TestLogEntry
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestLogEntry" /> class.
    /// </summary>
    /// <param name="level">The captured log level.</param>
    /// <param name="eventId">The captured event identifier.</param>
    /// <param name="message">The rendered log message.</param>
    /// <param name="exception">The captured exception, if any.</param>
    /// <param name="state">The structured log state values.</param>
    public TestLogEntry(
        LogLevel level,
        EventId eventId,
        string message,
        Exception? exception,
        IReadOnlyDictionary<string, object?> state
    )
    {
        Level = level;
        EventId = eventId;
        Message = message;
        Exception = exception;
        State = state;
    }

    /// <summary>
    ///     Gets the captured event identifier.
    /// </summary>
    public EventId EventId { get; }

    /// <summary>
    ///     Gets the captured exception, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    ///     Gets the captured log level.
    /// </summary>
    public LogLevel Level { get; }

    /// <summary>
    ///     Gets the rendered log message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     Gets the structured log state values.
    /// </summary>
    public IReadOnlyDictionary<string, object?> State { get; }
}