using System;
using System.Runtime.ExceptionServices;

using Microsoft.Extensions.Logging;


namespace Mississippi.Reservoir.Core.L0Tests;

/// <summary>
///     Throws a configured exception while checking or recording a log event.
/// </summary>
/// <param name="failure">The original logging failure.</param>
/// <param name="throwFromIsEnabled">Whether the enabled check throws instead of recording.</param>
internal sealed class StoreThrowingLogger(Exception failure, bool throwFromIsEnabled) : ILogger<Store>
{
    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(
        TState state
    )
        where TState : notnull =>
        null;

    /// <inheritdoc />
    public bool IsEnabled(
        LogLevel logLevel
    )
    {
        if (throwFromIsEnabled)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        return true;
    }

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        ExceptionDispatchInfo.Capture(failure).Throw();
    }
}