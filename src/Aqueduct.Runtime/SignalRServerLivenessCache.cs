using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Abstractions.Keys;
using Mississippi.Aqueduct.Runtime.Grains;

using Orleans;


namespace Mississippi.Aqueduct.Runtime;

/// <summary>
///     Shares pending and recent server-liveness queries within one silo.
/// </summary>
internal sealed class SignalRServerLivenessCache
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRServerLivenessCache" /> class.
    /// </summary>
    /// <param name="grainFactory">The factory for the shared server directory.</param>
    /// <param name="options">The heartbeat interval that bounds successful query reuse.</param>
    /// <param name="timeProvider">The monotonic clock used for cache expiry.</param>
    public SignalRServerLivenessCache(
        IGrainFactory grainFactory,
        IOptions<AqueductOptions> options,
        TimeProvider? timeProvider = null
    )
    {
        GrainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        ArgumentNullException.ThrowIfNull(options);
        CacheDuration = TimeSpan.FromMinutes(options.Value.HeartbeatIntervalMinutes);
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    private TimeSpan CacheDuration { get; }

    private IGrainFactory GrainFactory { get; }

    private Dictionary<(string ServerId, TimeSpan Timeout), (long CreatedAt, Task<bool> Query)> Queries { get; } = [];

    private object SyncRoot { get; } = new();

    private TimeProvider TimeProvider { get; }

    /// <summary>
    ///     Reuses a pending query or a successful response younger than one heartbeat interval.
    /// </summary>
    /// <param name="serverId">The gateway server to check.</param>
    /// <param name="timeout">The maximum heartbeat age accepted by the caller.</param>
    /// <returns>The shared directory response; failures remain retryable on the next call.</returns>
    public Task<bool> IsServerAliveAsync(
        string serverId,
        TimeSpan timeout
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(serverId);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        Task<bool> query;
        lock (SyncRoot)
        {
            long timestamp = TimeProvider.GetTimestamp();
            if (Queries.TryGetValue((serverId, timeout), out (long CreatedAt, Task<bool> Query) existing) &&
                ShouldRetain(existing, timestamp))
            {
                query = existing.Query;
            }
            else
            {
                (string ServerId, TimeSpan Timeout)[] expired = Queries
                    .Where(entry => !ShouldRetain(entry.Value, timestamp))
                    .Select(entry => entry.Key)
                    .ToArray();
                foreach ((string ServerId, TimeSpan Timeout) key in expired)
                {
                    Queries.Remove(key);
                }

                query = QueryServerAsync(serverId, timeout);
                Queries[(serverId, timeout)] = (timestamp, query);
            }
        }

        return query;
    }

    /// <summary>
    ///     Starts the asynchronous directory query without converting failures into a dead-server result.
    /// </summary>
    /// <param name="serverId">The gateway server to check.</param>
    /// <param name="timeout">The maximum heartbeat age accepted by the caller.</param>
    /// <returns>The directory response, including its original failure when unavailable.</returns>
    private async Task<bool> QueryServerAsync(
        string serverId,
        TimeSpan timeout
    ) =>
        await GrainFactory.GetGrain<ISignalRServerLivenessGrain>(SignalRServerDirectoryKey.Default)
            .IsServerAliveAsync(serverId, timeout)
            .ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

    /// <summary>
    ///     Keeps unfinished requests and successful responses within their bounded lifetime.
    /// </summary>
    /// <param name="entry">The cached query and its creation timestamp.</param>
    /// <param name="timestamp">The current monotonic timestamp.</param>
    /// <returns>Whether the query can still be shared.</returns>
    private bool ShouldRetain(
        (long CreatedAt, Task<bool> Query) entry,
        long timestamp
    ) =>
        !entry.Query.IsCompleted ||
        (entry.Query.IsCompletedSuccessfully &&
         (TimeProvider.GetElapsedTime(entry.CreatedAt, timestamp) < CacheDuration));
}