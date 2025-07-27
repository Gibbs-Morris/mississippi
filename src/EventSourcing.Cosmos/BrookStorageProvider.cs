using System.Runtime.CompilerServices;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.DependencyInjection;
using Mississippi.Core.Abstractions.Providers.Storage;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.EventSourcing.Cosmos.Abstractions;
using Mississippi.EventSourcing.Cosmos.Locking;
using Mississippi.EventSourcing.Cosmos.Batching;
using Mississippi.EventSourcing.Cosmos.Retry;
using Mississippi.EventSourcing.Cosmos.Streams;
using Mississippi.EventSourcing.Cosmos.Mapping;

namespace Mississippi.EventSourcing.Cosmos
{
    internal class BrookStorageProvider : IBrookStorageProvider
    {
        private IStreamRecoveryService RecoveryService { get; }
        private IEventStreamReader EventReader { get; }
        private IEventStreamAppender EventAppender { get; }

        internal BrookStorageProvider(
            IStreamRecoveryService recoveryService,
            IEventStreamReader eventReader,
            IEventStreamAppender eventAppender)
        {
            RecoveryService = recoveryService ?? throw new ArgumentNullException(nameof(recoveryService));
            EventReader = eventReader ?? throw new ArgumentNullException(nameof(eventReader));
            EventAppender = eventAppender ?? throw new ArgumentNullException(nameof(eventAppender));
        }

        public string Format => "cosmos-db";

        public async Task<BrookPosition> ReadHeadPositionAsync(BrookKey brookId, CancellationToken cancellationToken = default)
        {
            return await RecoveryService.GetOrRecoverHeadPositionAsync(brookId, cancellationToken);
        }

        public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(BrookRangeKey brookRange, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var brookEvent in EventReader.ReadEventsAsync(brookRange, cancellationToken))
            {
                yield return brookEvent;
            }
        }

        public async Task<BrookPosition> AppendEventsAsync(BrookKey brookId, IReadOnlyList<BrookEvent> events, BrookPosition? expectedVersion = null,
            CancellationToken cancellationToken = default)
        {
            if (events == null || events.Count == 0)
                throw new ArgumentException("Events collection cannot be null or empty", nameof(events));

            return await EventAppender.AppendEventsAsync(brookId, events, expectedVersion, cancellationToken);
        }
    }
}

namespace Mississippi.EventSourcing.Cosmos.Abstractions
{
}

namespace Mississippi.EventSourcing.Cosmos.Locking
{
}

namespace Mississippi.EventSourcing.Cosmos.Batching
{
}

namespace Mississippi.EventSourcing.Cosmos.Retry
{
}

namespace Mississippi.EventSourcing.Cosmos.Streams
{
}

namespace Mississippi.EventSourcing.Cosmos.Storage
{
}

namespace Mississippi.EventSourcing.Cosmos.Mapping
{
}

namespace Mississippi.EventSourcing.Cosmos
{
}
