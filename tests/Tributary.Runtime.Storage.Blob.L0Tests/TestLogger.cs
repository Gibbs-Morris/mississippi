using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Captures log entries for focused test assertions.
/// </summary>
/// <typeparam name="T">The logger category type.</typeparam>
internal sealed class TestLogger<T> : ILogger<T>
{
    private readonly List<TestLogEntry> entries = [];

    /// <summary>
    ///     Gets the log entries captured by this logger instance.
    /// </summary>
    public IReadOnlyList<TestLogEntry> Entries => entries;

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(
        TState state
    )
        where TState : notnull =>
        new NullScope();

    /// <inheritdoc />
    public bool IsEnabled(
        LogLevel logLevel
    ) =>
        true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        ArgumentNullException.ThrowIfNull(formatter);
        Dictionary<string, object?> structuredState = state is IEnumerable<KeyValuePair<string, object?>> pairs
            ? pairs.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
            : [];
        entries.Add(new(logLevel, eventId, formatter(state, exception), exception, structuredState));
    }

    private sealed class NullScope : IDisposable
    {
        /// <summary>
        ///     Releases the scope.
        /// </summary>
        public void Dispose()
        {
        }
    }
}