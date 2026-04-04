using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Loads saga recovery inputs and delegates resume planning to the runtime planner.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class SagaRecoveryCoordinator<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaRecoveryCoordinator{TSaga}" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">Factory for loading the current saga state.</param>
    /// <param name="checkpointAccessor">Accessor for the latest authoritative recovery checkpoint.</param>
    /// <param name="planner">Planner that converts loaded recovery state into a recovery decision.</param>
    public SagaRecoveryCoordinator(
        IAggregateGrainFactory aggregateGrainFactory,
        SagaRecoveryCheckpointAccessor<TSaga> checkpointAccessor,
        SagaRecoveryPlanner<TSaga> planner
    )
    {
        ArgumentNullException.ThrowIfNull(aggregateGrainFactory);
        ArgumentNullException.ThrowIfNull(checkpointAccessor);
        ArgumentNullException.ThrowIfNull(planner);
        AggregateGrainFactory = aggregateGrainFactory;
        CheckpointAccessor = checkpointAccessor;
        Planner = planner;
    }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    private SagaRecoveryCheckpointAccessor<TSaga> CheckpointAccessor { get; }

    private SagaRecoveryPlanner<TSaga> Planner { get; }

    /// <summary>
    ///     Loads the current saga state and checkpoint, then computes the next recovery action.
    /// </summary>
    /// <param name="entityId">The saga entity identifier.</param>
    /// <param name="source">The source requesting recovery planning.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The recovery plan selected for the current saga instance.</returns>
    public async Task<SagaRecoveryPlan> PlanAsync(
        string entityId,
        SagaResumeSource source,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        TSaga? state = await AggregateGrainFactory.GetGenericAggregate<TSaga>(entityId)
            .GetStateAsync(cancellationToken);
        if (state is null)
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.NoAction,
                Reason = "Saga state not found.",
            };
        }

        if (state.Phase is SagaPhase.Completed or SagaPhase.Compensated or SagaPhase.Failed)
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.Terminal,
            };
        }

        SagaRecoveryCheckpoint? checkpoint = await CheckpointAccessor.GetAsync(entityId, cancellationToken);
        if (checkpoint is null)
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.NoAction,
                Reason = "Recovery checkpoint not found.",
            };
        }

        return Planner.Plan(state, checkpoint, source);
    }
}