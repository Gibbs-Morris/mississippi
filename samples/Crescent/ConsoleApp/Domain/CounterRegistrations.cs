using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Reducers;


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
        return services;
    }
}