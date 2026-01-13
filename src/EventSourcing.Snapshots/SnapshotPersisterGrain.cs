using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Diagnostics;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.EventSourcing.Snapshots;

/// <summary>
///     Grain that handles background persistence of snapshots to storage.
/// </summary>
/// <remarks>
///     <para>
///         This grain is designed to receive fire-and-forget calls from
///         <see cref="SnapshotCacheGrain{TSnapshot}" />,
///         allowing the cache grain to return immediately after building state
///         while persistence happens asynchronously.
///     </para>
///     <para>
///         The grain is keyed by <see cref="SnapshotKey" /> in the format
///         "brookName|entityId|version|snapshotStorageName|reducersHash",
///         matching the cache grain's key for one-to-one correspondence.
///     </para>
/// </remarks>
internal sealed class SnapshotPersisterGrain
    : ISnapshotPersisterGrain,
      IGrainBase
{
    private SnapshotKey snapshotKey;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotPersisterGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="snapshotStorageWriter">Writer for persisting snapshots to storage.</param>
    /// <param name="logger">Logger instance.</param>
    public SnapshotPersisterGrain(
        IGrainContext grainContext,
        ISnapshotStorageWriter snapshotStorageWriter,
        ILogger<SnapshotPersisterGrain> logger
    )
    {
        GrainContext = grainContext ?? throw new ArgumentNullException(nameof(grainContext));
        SnapshotStorageWriter = snapshotStorageWriter ?? throw new ArgumentNullException(nameof(snapshotStorageWriter));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IGrainContext GrainContext { get; }

    /// <summary>
    ///     Gets the logger instance.
    /// </summary>
    private ILogger<SnapshotPersisterGrain> Logger { get; }

    /// <summary>
    ///     Gets the snapshot storage writer.
    /// </summary>
    private ISnapshotStorageWriter SnapshotStorageWriter { get; }

    /// <inheritdoc />
    public Task OnActivateAsync(
        CancellationToken token
    )
    {
        string primaryKey = this.GetPrimaryKeyString();
        snapshotKey = SnapshotKey.FromString(primaryKey);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task PersistAsync(
        SnapshotEnvelope envelope,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(envelope);
        string keyString = snapshotKey;
        string snapshotTypeName = snapshotKey.Stream.SnapshotStorageName;
        Logger.PersistingSnapshot(keyString);
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            await SnapshotStorageWriter.WriteAsync(snapshotKey, envelope, cancellationToken);
            sw.Stop();
            SnapshotMetrics.RecordPersist(snapshotTypeName, sw.Elapsed.TotalMilliseconds, true);
            Logger.SnapshotPersisted(keyString);
        }
        catch (Exception ex)
        {
            sw.Stop();
            SnapshotMetrics.RecordPersist(snapshotTypeName, sw.Elapsed.TotalMilliseconds, false);
            Logger.PersistenceFailed(ex, keyString, ex.Message);
            throw;
        }
    }
}