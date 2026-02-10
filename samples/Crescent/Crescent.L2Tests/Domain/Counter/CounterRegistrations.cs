using Crescent.Crescent.L2Tests.Domain.Counter.Commands;
using Crescent.Crescent.L2Tests.Domain.Counter.Events;
using Crescent.Crescent.L2Tests.Domain.Counter.Handlers;
using Crescent.Crescent.L2Tests.Domain.Counter.Reducers;
using Crescent.Crescent.L2Tests.Domain.CounterSummary;
using Crescent.Crescent.L2Tests.Domain.CounterSummary.Reducers;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Reducers;
using Mississippi.EventSourcing.Snapshots;
using Mississippi.EventSourcing.UxProjections;


namespace Crescent.Crescent.L2Tests.Domain.Counter;

/// <summary>
///     Extension methods for registering counter aggregate services.
/// </summary>
internal static class CounterRegistrations
{
    /// <summary>
    ///     Adds the counter aggregate services to the Mississippi silo builder.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddCounterAggregate(
        this IMississippiSiloBuilder builder
    )
    {
        // Add aggregate infrastructure
        builder.AddAggregateSupport();

        // Register event types for hydration
        builder.AddEventType<CounterInitialized>();
        builder.AddEventType<CounterIncremented>();
        builder.AddEventType<CounterDecremented>();
        builder.AddEventType<CounterReset>();

        // Register command handlers
        builder.AddCommandHandler<InitializeCounter, CounterAggregate, InitializeCounterHandler>();
        builder.AddCommandHandler<IncrementCounter, CounterAggregate, IncrementCounterHandler>();
        builder.AddCommandHandler<DecrementCounter, CounterAggregate, DecrementCounterHandler>();
        builder.AddCommandHandler<ResetCounter, CounterAggregate, ResetCounterHandler>();

        // Register reducers for state computation
        builder.AddReducer<CounterInitialized, CounterAggregate, CounterInitializedEventReducer>();
        builder.AddReducer<CounterIncremented, CounterAggregate, CounterIncrementedEventReducer>();
        builder.AddReducer<CounterDecremented, CounterAggregate, CounterDecrementedEventReducer>();
        builder.AddReducer<CounterReset, CounterAggregate, CounterResetEventReducer>();

        // Add snapshot state converter for CounterAggregate (required for aggregate snapshots)
        builder.AddSnapshotStateConverter<CounterAggregate>();

        // Register reducers for CounterSummaryProjection (UX projection)
        builder.AddReducer<CounterInitialized, CounterSummaryProjection, CounterSummaryInitializedEventReducer>();
        builder.AddReducer<CounterIncremented, CounterSummaryProjection, CounterSummaryIncrementedEventReducer>();
        builder.AddReducer<CounterDecremented, CounterSummaryProjection, CounterSummaryDecrementedEventReducer>();
        builder.AddReducer<CounterReset, CounterSummaryProjection, CounterSummaryResetEventReducer>();

        // Add snapshot state converter for CounterSummaryProjection (required for projection verification)
        builder.AddSnapshotStateConverter<CounterSummaryProjection>();

        // Add UX projections infrastructure for read-optimized views.
        // This enables the CounterSummaryProjectionGrain to serve cached projections.
        // Multiple projection types (like CounterSummaryProjection) can consume the same
        // CounterBrook event stream, each with their own key and cache.
        builder.AddUxProjections();
        return builder;
    }
}