using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Core.Abstractions.Mapping;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Cosmos.Abstractions;
using Mississippi.EventSourcing.Brooks.Cosmos.Retry;
using Mississippi.EventSourcing.Brooks.Cosmos.Storage;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Brooks;

/// <summary>
///     Cosmos DB implementation of the event brook reader for reading events from brooks.
/// </summary>
internal class EventBrookReader : IEventBrookReader
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventBrookReader" /> class.
    /// </summary>
    /// <param name="repository">The Cosmos repository for low-level operations.</param>
    /// <param name="retryPolicy">The retry policy for handling transient failures.</param>
    /// <param name="options">The configuration options for brook storage.</param>
    /// <param name="eventMapper">The mapper for converting storage models to events.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public EventBrookReader(
        ICosmosRepository repository,
        IRetryPolicy retryPolicy,
        IOptions<BrookStorageOptions> options,
        IMapper<EventStorageModel, BrookEvent> eventMapper,
        ILogger<EventBrookReader> logger
    )
    {
        Repository = repository;
        RetryPolicy = retryPolicy;
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        EventMapper = eventMapper;
        Logger = logger;
    }

    private IMapper<EventStorageModel, BrookEvent> EventMapper { get; }

    private ILogger<EventBrookReader> Logger { get; }

    private BrookStorageOptions Options { get; }

    private ICosmosRepository Repository { get; }

    private IRetryPolicy RetryPolicy { get; }

    /// <summary>
    ///     Reads events from the specified brook range asynchronously.
    /// </summary>
    /// <param name="brookRange">The brook range specifying which events to read.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An async enumerable of brook events within the specified range.</returns>
    public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookRangeKey brookRange,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        Logger.ReadingEvents(brookRange);
        int eventCount = 0;
        await foreach (EventStorageModel eventStorageModel in Repository.QueryEventsAsync(
                           brookRange,
                           Options.QueryBatchSize,
                           cancellationToken))
        {
            BrookEvent brookEvent = EventMapper.Map(eventStorageModel);
            eventCount++;
            yield return brookEvent;
        }

        Logger.ReadCompleted(brookRange, eventCount);
    }
}