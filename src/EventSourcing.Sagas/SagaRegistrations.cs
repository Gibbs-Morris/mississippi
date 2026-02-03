using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Reducers;
using Mississippi.EventSourcing.Sagas.Abstractions;

namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Provides extension methods for registering saga orchestration components.
/// </summary>
public static class SagaRegistrations
{
    /// <summary>
    ///     Adds saga orchestration infrastructure to the service collection.
    /// </summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <typeparam name="TInput">The saga input type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSagaOrchestration<TSaga, TInput>(
        this IServiceCollection services
    )
        where TSaga : class, ISagaState
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddAggregateSupport();
        services.AddRootEventEffect<TSaga>();
        services.AddRootReducer<TSaga>();
        services.AddTransient<IEventEffect<TSaga>, SagaOrchestrationEffect<TSaga>>();
        return services;
    }
}
