using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure.Brooks;
using Mississippi.Brooks.Runtime.Storage.Azure.Storage;


namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     Azure Blob Storage implementation of the Brooks storage provider.
/// </summary>
internal sealed class BrookStorageProvider : IBrookStorageProvider
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookStorageProvider" /> class.
    /// </summary>
    /// <param name="recoveryService">The recovery service used before reads and writes.</param>
    /// <param name="repository">The repository used for committed event reads.</param>
    /// <param name="eventWriter">The event writer used for append orchestration.</param>
    /// <param name="logger">The logger used for provider diagnostics.</param>
    public BrookStorageProvider(
        IBrookRecoveryService recoveryService,
        IAzureBrookRepository repository,
        IEventBrookWriter eventWriter,
        ILogger<BrookStorageProvider> logger
    )
    {
        RecoveryService = recoveryService ?? throw new ArgumentNullException(nameof(recoveryService));
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        EventWriter = eventWriter ?? throw new ArgumentNullException(nameof(eventWriter));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IEventBrookWriter EventWriter { get; }

    private ILogger<BrookStorageProvider> Logger { get; }

    private IAzureBrookRepository Repository { get; }

    private IBrookRecoveryService RecoveryService { get; }

    /// <inheritdoc />
    public string Format => "azure-blob";

    /// <inheritdoc />
    public Task<BrookPosition> AppendEventsAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition? expectedVersion = null,
        CancellationToken cancellationToken = default
    )
    {
        if ((events == null) || (events.Count == 0))
        {
            throw new ArgumentException("Events collection cannot be null or empty", nameof(events));
        }

        Logger.AppendingEvents(brookId, events.Count);
        return AppendEventsCoreAsync(brookId, events, expectedVersion, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BrookPosition> ReadCursorPositionAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    )
    {
        Logger.ReadingCursorPosition(brookId);

        try
        {
            BrookPosition position = await RecoveryService.GetOrRecoverCursorPositionAsync(brookId, cancellationToken)
                .ConfigureAwait(false);
            Logger.ReadCursorPositionCompleted(brookId, position.Value);
            return position;
        }
        catch (Exception exception)
        {
            Logger.ReadCursorPositionFailed(exception, brookId);
            throw;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookRangeKey brookRange,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        Logger.ReadingEvents(brookRange);

        BrookPosition cursorPosition;
        try
        {
            cursorPosition = await RecoveryService.GetOrRecoverCursorPositionAsync(
                    brookRange.ToBrookCompositeKey(),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logger.ReadEventsFailed(exception, brookRange);
            throw;
        }

        if (cursorPosition.Value < brookRange.Start.Value)
        {
            Logger.ReadEventsResolvedRange(brookRange, 0);
            yield break;
        }

        long availableCount = Math.Min(brookRange.Count, (cursorPosition.Value - brookRange.Start.Value) + 1);
        if (availableCount <= 0)
        {
            Logger.ReadEventsResolvedRange(brookRange, 0);
            yield break;
        }

        BrookRangeKey committedRange = new(
            brookRange.BrookName,
            brookRange.EntityId,
            brookRange.Start.Value,
            availableCount);

        Logger.ReadEventsResolvedRange(committedRange, availableCount);

        IAsyncEnumerator<BrookEvent> enumerator = Repository.ReadEventsAsync(committedRange, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        await using (enumerator.ConfigureAwait(false))
        {
            while (true)
            {
                BrookEvent brookEvent;
                try
                {
                    if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        break;
                    }

                    brookEvent = enumerator.Current;
                }
                catch (Exception exception)
                {
                    Logger.ReadEventsFailed(exception, committedRange);
                    throw;
                }

                yield return brookEvent;
            }
        }
    }

    private async Task<BrookPosition> AppendEventsCoreAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition? expectedVersion,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            BrookPosition position = await EventWriter.AppendEventsAsync(
                    brookId,
                    events,
                    expectedVersion,
                    cancellationToken)
                .ConfigureAwait(false);
            Logger.AppendEventsCompleted(brookId, position.Value);
            return position;
        }
        catch (Exception exception)
        {
            Logger.AppendEventsFailed(exception, brookId);
            throw;
        }
    }
}