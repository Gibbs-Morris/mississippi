using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;


namespace Mississippi.Reservoir.Core.L0Tests;

/// <summary>
///     Captures store logging while allowing tests to disable error events.
/// </summary>
/// <param name="enabled">Whether logging is enabled.</param>
internal sealed class StoreCapturingLogger(bool enabled = true) : ILogger<Store>
{
    /// <summary>
    ///     Gets the captured log entries.
    /// </summary>
    public List<StoreCapturedLog> Entries { get; } = [];

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(
        TState state
    )
        where TState : notnull =>
        null;

    /// <inheritdoc />
    public bool IsEnabled(
        LogLevel logLevel
    ) =>
        enabled;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        KeyValuePair<string, object?>[] fields = state is IEnumerable<KeyValuePair<string, object?>> values
            ? values.ToArray()
            : [];
        Entries.Add(new(logLevel, eventId, formatter(state, exception), exception, fields));
    }
}