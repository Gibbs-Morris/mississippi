using System.Threading.Tasks;

using Crescent.ConsoleApp.Counter.Commands;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Orleans.Runtime;


namespace Crescent.ConsoleApp.Counter;

/// <summary>
///     Aggregate grain implementation for the counter domain.
/// </summary>
internal sealed class CounterAggregateGrain
    : AggregateGrain<CounterState, CounterBrook>,
      ICounterAggregateGrain
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CounterAggregateGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="brookGrainFactory">Factory for resolving brook grains.</param>
    /// <param name="brookEventConverter">Converter for domain events to/from brook events.</param>
    /// <param name="rootCommandHandler">The root command handler for processing commands.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving snapshot grains.</param>
    /// <param name="rootReducer">The root reducer for obtaining the reducers hash.</param>
    /// <param name="logger">Logger instance.</param>
    public CounterAggregateGrain(
        IGrainContext grainContext,
        IBrookGrainFactory brookGrainFactory,
        IBrookEventConverter brookEventConverter,
        IRootCommandHandler<CounterState> rootCommandHandler,
        ISnapshotGrainFactory snapshotGrainFactory,
        IRootReducer<CounterState> rootReducer,
        ILogger<CounterAggregateGrain> logger
    )
        : base(
            grainContext,
            brookGrainFactory,
            brookEventConverter,
            rootCommandHandler,
            snapshotGrainFactory,
            rootReducer.GetReducerHash(),
            logger)
    {
    }

    /// <inheritdoc />
    public Task<OperationResult> DecrementAsync(
        int amount = 1
    ) =>
        ExecuteAsync(
            new DecrementCounter
            {
                Amount = amount,
            });

    /// <inheritdoc />
    public Task<OperationResult> IncrementAsync(
        int amount = 1
    ) =>
        ExecuteAsync(
            new IncrementCounter
            {
                Amount = amount,
            });

    /// <inheritdoc />
    public Task<OperationResult> InitializeAsync(
        int initialValue = 0
    ) =>
        ExecuteAsync(
            new InitializeCounter
            {
                InitialValue = initialValue,
            });

    /// <inheritdoc />
    public Task<OperationResult> ResetAsync(
        int newValue = 0
    ) =>
        ExecuteAsync(
            new ResetCounter
            {
                NewValue = newValue,
            });
}
