using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Serialization.Abstractions;

using Orleans.Runtime;


namespace Crescent.ConsoleApp.Domain;

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
    /// <param name="serializationProvider">Provider for event serialization.</param>
    /// <param name="rootReducer">The root reducer for computing state from events.</param>
    /// <param name="logger">Logger instance.</param>
    public CounterAggregateGrain(
        IGrainContext grainContext,
        IBrookGrainFactory brookGrainFactory,
        ISerializationProvider serializationProvider,
        IRootReducer<CounterState> rootReducer,
        ILogger<CounterAggregateGrain> logger
    )
        : base(grainContext, brookGrainFactory, serializationProvider, rootReducer, logger)
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