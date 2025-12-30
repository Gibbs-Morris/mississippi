using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Storage;


namespace Mississippi.Testing.Utilities.Storage;

/// <summary>
///     In-memory implementation of <see cref="IBrookStorageReader" /> and <see cref="IBrookStorageWriter" />
///     for testing purposes.
/// </summary>
/// <remarks>
///     This class provides a simple, thread-safe in-memory event store that can be used in unit tests
///     without requiring actual storage infrastructure.
/// </remarks>
public class InMemoryBrookStorage
    : IBrookStorageReader,
      IBrookStorageWriter
{
    private readonly Dictionary<string, long> cursors = new();

    private readonly Dictionary<string, List<BrookEvent>> eventsByKey = new();

    private readonly object syncLock = new();

    /// <inheritdoc />
    public Task<BrookPosition> AppendEventsAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition? expectedVersion = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(events);
        lock (syncLock)
        {
            string key = brookId;
            if (!eventsByKey.TryGetValue(key, out List<BrookEvent>? list))
            {
                list = new();
                eventsByKey[key] = list;
            }

            // Cursor represents last written position (-1 for empty brook)
            long cursor = cursors.TryGetValue(key, out long v) ? v : -1;
            if (expectedVersion.HasValue && (expectedVersion.Value.Value != cursor))
            {
                throw new InvalidOperationException(
                    $"Expected version {expectedVersion.Value.Value} but current position is {cursor}.");
            }

            list.AddRange(events);

            // After appending, cursor is the position of the last event written
            cursor += events.Count;
            cursors[key] = cursor;
            return Task.FromResult(new BrookPosition(cursor));
        }
    }

    /// <summary>
    ///     Clears all stored events and cursors.
    /// </summary>
    /// <remarks>
    ///     This method is useful for resetting state between tests.
    /// </remarks>
    public void Clear()
    {
        lock (syncLock)
        {
            cursors.Clear();
            eventsByKey.Clear();
        }
    }

    /// <summary>
    ///     Gets the count of events stored for a specific brook.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <returns>The number of events stored for the brook, or 0 if the brook doesn't exist.</returns>
    public int GetEventCount(
        BrookKey brookId
    )
    {
        lock (syncLock)
        {
            return eventsByKey.TryGetValue(brookId, out List<BrookEvent>? list) ? list.Count : 0;
        }
    }

    /// <inheritdoc />
    public Task<BrookPosition> ReadCursorPositionAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    )
    {
        lock (syncLock)
        {
            if (cursors.TryGetValue(brookId, out long v))
            {
                return Task.FromResult(new BrookPosition(v));
            }

            // Return -1 (NotSet) for streams that don't exist yet
            return Task.FromResult(new BrookPosition(-1));
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookRangeKey brookRange,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        List<BrookEvent>? snapshot;
        lock (syncLock)
        {
            string key = brookRange.ToBrookCompositeKey();
            if (!eventsByKey.TryGetValue(key, out List<BrookEvent>? list))
            {
                yield break;
            }

            // Create a snapshot to avoid holding the lock during enumeration
            snapshot = new(list);
        }

        long start = brookRange.Start;
        long end = brookRange.End;
        for (int i = (int)start; (i < snapshot.Count) && (i <= end); i++)
        {
            yield return snapshot[i];
        }

        await Task.CompletedTask;
    }

    /// <summary>
    ///     Forces the cursor position for the specified brook in the in-memory store.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="position">The cursor position to set.</param>
    /// <remarks>
    ///     This method is useful for setting up test scenarios with pre-existing cursor positions.
    /// </remarks>
    public void SetCursor(
        BrookKey brookId,
        long position
    )
    {
        lock (syncLock)
        {
            cursors[(string)brookId] = position;
        }
    }
}