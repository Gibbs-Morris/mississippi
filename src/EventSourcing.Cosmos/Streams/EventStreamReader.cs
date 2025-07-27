using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Mississippi.Core.Abstractions.Mapping;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.EventSourcing.Cosmos.Abstractions;
using Mississippi.EventSourcing.Cosmos.Retry;
using Mississippi.EventSourcing.Cosmos.Storage;

namespace Mississippi.EventSourcing.Cosmos.Streams;

internal class EventStreamReader : IEventStreamReader
{
    private ICosmosRepository Repository { get; }
    private IRetryPolicy RetryPolicy { get; }
    private BrookStorageOptions Options { get; }
    private IMapper<EventStorageModel, BrookEvent> EventMapper { get; }

    public EventStreamReader(ICosmosRepository repository, IRetryPolicy retryPolicy, IOptions<BrookStorageOptions> options, IMapper<EventStorageModel, BrookEvent> eventMapper)
    {
        Repository = repository;
        RetryPolicy = retryPolicy;
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        EventMapper = eventMapper;
    }

    public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(BrookRangeKey brookRange, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var eventStorageModel in Repository.QueryEventsAsync(brookRange, Options.QueryBatchSize, cancellationToken))
        {
            yield return EventMapper.Map(eventStorageModel);
        }
    }
}