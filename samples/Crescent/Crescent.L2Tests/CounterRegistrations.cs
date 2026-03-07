using Mississippi.DomainModeling.Runtime;
using Mississippi.Tributary.Runtime;


namespace Crescent.Crescent.L2Tests;

/// <summary>
///     Extension methods for registering counter aggregate services.
/// </summary>
internal static class CounterRegistrations
{
    /// <summary>
    ///     Adds the counter aggregate services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCounterAggregate(
        this IServiceCollection services
    )
    {
        // Add aggregate infrastructure
        services.AddAggregateSupport();

        // Register event types for hydration
        services.AddEventType<CounterInitialized>();
        services.AddEventType<CounterIncremented>();
        services.AddEventType<CounterDecremented>();
        services.AddEventType<CounterReset>();

        // Register command handlers
        services.AddCommandHandler<InitializeCounter, CounterAggregate, InitializeCounterHandler>();
        services.AddCommandHandler<IncrementCounter, CounterAggregate, IncrementCounterHandler>();
        services.AddCommandHandler<DecrementCounter, CounterAggregate, DecrementCounterHandler>();
        services.AddCommandHandler<ResetCounter, CounterAggregate, ResetCounterHandler>();

        // Register reducers for state computation
        services.AddReducer<CounterInitialized, CounterAggregate, CounterInitializedEventReducer>();
        services.AddReducer<CounterIncremented, CounterAggregate, CounterIncrementedEventReducer>();
        services.AddReducer<CounterDecremented, CounterAggregate, CounterDecrementedEventReducer>();
        services.AddReducer<CounterReset, CounterAggregate, CounterResetEventReducer>();

        // Add snapshot state converter for CounterAggregate (required for aggregate snapshots)
        services.AddSnapshotStateConverter<CounterAggregate>();

        // Register reducers for CounterSummaryProjection (UX projection)
        services.AddReducer<CounterInitialized, CounterSummaryProjection, CounterSummaryInitializedEventReducer>();
        services.AddReducer<CounterIncremented, CounterSummaryProjection, CounterSummaryIncrementedEventReducer>();
        services.AddReducer<CounterDecremented, CounterSummaryProjection, CounterSummaryDecrementedEventReducer>();
        services.AddReducer<CounterReset, CounterSummaryProjection, CounterSummaryResetEventReducer>();

        // Add snapshot state converter for CounterSummaryProjection (required for projection verification)
        services.AddSnapshotStateConverter<CounterSummaryProjection>();

        // Add UX projections infrastructure for read-optimized views.
        // This enables the CounterSummaryProjectionGrain to serve cached projections.
        // Multiple projection types (like CounterSummaryProjection) can consume the same
        // CounterBrook event stream, each with their own key and cache.
        services.AddUxProjections();
        return services;
    }
}