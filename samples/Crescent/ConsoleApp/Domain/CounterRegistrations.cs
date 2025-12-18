using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Reducers;
using Mississippi.EventSourcing.UxProjections;


namespace Crescent.ConsoleApp.Domain;

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
        services.AddCommandHandler<InitializeCounter, CounterState, InitializeCounterHandler>();
        services.AddCommandHandler<IncrementCounter, CounterState, IncrementCounterHandler>();
        services.AddCommandHandler<DecrementCounter, CounterState, DecrementCounterHandler>();
        services.AddCommandHandler<ResetCounter, CounterState, ResetCounterHandler>();

        // Register reducers for state computation
        services.AddReducer<CounterInitialized, CounterState, CounterInitializedReducer>();
        services.AddReducer<CounterIncremented, CounterState, CounterIncrementedReducer>();
        services.AddReducer<CounterDecremented, CounterState, CounterDecrementedReducer>();
        services.AddReducer<CounterReset, CounterState, CounterResetReducer>();

        // Add UX projections infrastructure for read-optimized views.
        // This enables the CounterSummaryProjectionGrain to serve cached projections.
        // Multiple projection types (like CounterSummaryProjection) can consume the same
        // CounterBrook event stream, each with their own key and cache.
        services.AddUxProjections();
        return services;
    }
}