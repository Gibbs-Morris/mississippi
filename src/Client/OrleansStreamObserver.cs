using System.Reactive.Subjects;

using Microsoft.Extensions.Logging;

using Orleans.Streams;


namespace Mississippi.Client;

internal sealed class OrleansStreamObserver<T> : IAsyncObserver<T>
{
    private static readonly Action<ILogger, T, StreamSequenceToken?, Exception?> ReceivedItemMessage =
        LoggerMessage.Define<T, StreamSequenceToken?>(
            LogLevel.Information,
            new(1000, nameof(OnNextAsync)),
            "Received item: {Item}. Sequence token: {Token}");

    private static readonly Action<ILogger, Exception?, Exception?> StreamErrorMessage =
        LoggerMessage.Define<Exception?>(LogLevel.Error, new(1001, nameof(OnErrorAsync)), "Stream error occurred");

    private static readonly Action<ILogger, Exception?> StreamCompletedMessage = LoggerMessage.Define(
        LogLevel.Information,
        new(1002, nameof(OnCompletedAsync)),
        "Stream completed");

    public OrleansStreamObserver(
        ISubject<T> subject,
        ILogger<OrleansStreamObserver<T>> logger
    )
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(logger);
        Subject = subject;
        Logger = logger;
    }

    private ILogger<OrleansStreamObserver<T>> Logger { get; }

    private ISubject<T> Subject { get; }

    public Task OnNextAsync(
        T item,
        StreamSequenceToken? token = null
    )
    {
        ReceivedItemMessage(Logger, item, token, null);
        Subject.OnNext(item);
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(
        Exception ex
    )
    {
        StreamErrorMessage(Logger, ex, null);
        Subject.OnError(ex);
        return Task.CompletedTask;
    }

    public Task OnCompletedAsync()
    {
        StreamCompletedMessage(Logger, null);
        Subject.OnCompleted();
        return Task.CompletedTask;
    }
}