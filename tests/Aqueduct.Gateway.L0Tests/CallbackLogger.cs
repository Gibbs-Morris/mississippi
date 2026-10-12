using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Aqueduct.Gateway.L0Tests;

/// <summary>
///     Signals completed log writes so asynchronous failure observation can be tested without polling.
/// </summary>
/// <typeparam name="TCategory">The logger category.</typeparam>
internal sealed class CallbackLogger<TCategory> : ILogger<TCategory>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CallbackLogger{TCategory}" /> class.
    /// </summary>
    /// <param name="onLog">The callback receiving the event and exception from each log write.</param>
    public CallbackLogger(
        Action<EventId, Exception?> onLog
    ) =>
        OnLog = onLog ?? throw new ArgumentNullException(nameof(onLog));

    private Action<EventId, Exception?> OnLog { get; }

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
        true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    ) =>
        OnLog(eventId, exception);
}