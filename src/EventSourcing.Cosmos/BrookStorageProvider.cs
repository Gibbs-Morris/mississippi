using System.Runtime.CompilerServices;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.DependencyInjection;
using Mississippi.Core.Abstractions.Providers.Storage;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.EventSourcing.Cosmos.Abstractions;
using Mississippi.EventSourcing.Cosmos.Batching;
using Mississippi.EventSourcing.Cosmos.Locking;
using Mississippi.EventSourcing.Cosmos.Mapping;
using Mississippi.EventSourcing.Cosmos.Retry;

namespace Mississippi.EventSourcing.Cosmos
{
    /// <summary>
    /// Cosmos DB implementation of the Brook storage provider for event streaming.
    /// </summary>
    internal class BrookStorageProvider : IBrookStorageProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BrookStorageProvider"/> class.
        /// </summary>
        /// <param name="recoveryService">The brook recovery service for managing head positions.</param>
        /// <param name="eventReader">The event reader for reading events from brooks.</param>
        /// <param name="eventAppender">The event appender for writing events to brooks.</param>
        internal BrookStorageProvider(
            IBrookRecoveryService recoveryService,
            IEventBrookReader eventReader,
            IEventBrookAppender eventAppender)
        {
            RecoveryService = recoveryService ?? throw new ArgumentNullException(nameof(recoveryService));
            EventReader = eventReader ?? throw new ArgumentNullException(nameof(eventReader));
            EventAppender = eventAppender ?? throw new ArgumentNullException(nameof(eventAppender));
        }

        /// <summary>
        /// Gets the brook recovery service for managing head positions.
        /// </summary>
        private IBrookRecoveryService RecoveryService { get; }

        /// <summary>
        /// Gets the event reader for reading events from brooks.
        /// </summary>
        private IEventBrookReader EventReader { get; }

        /// <summary>
        /// Gets the event appender for writing events to brooks.
        /// </summary>
        private IEventBrookAppender EventAppender { get; }

        /// <summary>
        /// Gets the format identifier for this storage provider.
        /// </summary>
        public string Format => "cosmos-db";

        /// <summary>
        /// Reads the current head position of the specified brook stream.
        /// </summary>
        /// <param name="brookId">The brook identifier specifying the target stream.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The current head position of the stream.</returns>
        public async Task<BrookPosition> ReadHeadPositionAsync(BrookKey brookId, CancellationToken cancellationToken = default)
        {
            return await RecoveryService.GetOrRecoverHeadPositionAsync(brookId, cancellationToken);
        }

        /// <summary>
        /// Reads events from the specified brook range asynchronously.
        /// </summary>
        /// <param name="brookRange">The brook range specifying which events to read.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>An async enumerable of brook events within the specified range.</returns>
        public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(BrookRangeKey brookRange, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var brookEvent in EventReader.ReadEventsAsync(brookRange, cancellationToken))
            {
                yield return brookEvent;
            }
        }

        /// <summary>
        /// Appends a collection of events to the specified brook stream.
        /// </summary>
        /// <param name="brookId">The brook identifier specifying the target stream.</param>
        /// <param name="events">The collection of events to append to the stream.</param>
        /// <param name="expectedVersion">The expected version for optimistic concurrency control.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The position after successfully appending all events.</returns>
        public async Task<BrookPosition> AppendEventsAsync(
            BrookKey brookId,
            IReadOnlyList<BrookEvent> events,
            BrookPosition? expectedVersion = null,
            CancellationToken cancellationToken = default)
        {
            if (events == null || events.Count == 0)
            {
                throw new ArgumentException("Events collection cannot be null or empty", nameof(events));
            }

            return await EventAppender.AppendEventsAsync(brookId, events, expectedVersion, cancellationToken);
        }
    }
}
