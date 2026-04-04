using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Loads the latest framework-owned recovery checkpoint snapshot for a saga.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class SagaRecoveryCheckpointAccessor<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaRecoveryCheckpointAccessor{TSaga}" /> class.
    /// </summary>
    /// <param name="brookGrainFactory">Factory for resolving brook cursor grains.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot cache grains.</param>
    /// <param name="checkpointReducer">Root reducer used to scope checkpoint snapshot compatibility.</param>
    public SagaRecoveryCheckpointAccessor(
        IBrookGrainFactory brookGrainFactory,
        ISnapshotGrainFactory snapshotGrainFactory,
        IRootReducer<SagaRecoveryCheckpoint> checkpointReducer
    )
    {
        ArgumentNullException.ThrowIfNull(brookGrainFactory);
        ArgumentNullException.ThrowIfNull(snapshotGrainFactory);
        ArgumentNullException.ThrowIfNull(checkpointReducer);
        BrookGrainFactory = brookGrainFactory;
        SnapshotGrainFactory = snapshotGrainFactory;
        CheckpointReducer = checkpointReducer;
    }

    private IBrookGrainFactory BrookGrainFactory { get; }

    private IRootReducer<SagaRecoveryCheckpoint> CheckpointReducer { get; }

    private ISnapshotGrainFactory SnapshotGrainFactory { get; }

    /// <summary>
    ///     Loads the latest recovery checkpoint snapshot for the specified saga entity identifier.
    /// </summary>
    /// <param name="entityId">The saga entity identifier.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The latest recovery checkpoint, or <c>null</c> when the saga brook has no events.</returns>
    public async Task<SagaRecoveryCheckpoint?> GetAsync(
        string entityId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        BrookKey brookKey = BrookKey.ForType<TSaga>(entityId);
        BrookPosition currentPosition = await BrookGrainFactory.GetBrookCursorGrain(brookKey).GetLatestPositionAsync();
        if (currentPosition.NotSet)
        {
            return null;
        }

        SnapshotStreamKey streamKey = new(
            brookKey.BrookName,
            SnapshotStorageNameHelper.GetStorageName<SagaRecoveryCheckpoint>(),
            brookKey.EntityId,
            CheckpointReducer.GetReducerHash());
        SnapshotKey snapshotKey = new(streamKey, currentPosition.Value);
        return await SnapshotGrainFactory.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(snapshotKey)
            .GetStateAsync(cancellationToken);
    }
}