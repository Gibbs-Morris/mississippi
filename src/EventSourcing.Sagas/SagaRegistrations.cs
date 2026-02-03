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
        services.AddEventType<SagaStartedEvent>();
        services.AddEventType<SagaStepCompleted>();
        services.AddEventType<SagaStepFailed>();
        services.AddEventType<SagaCompensating>();
        services.AddEventType<SagaStepCompensated>();
        services.AddEventType<SagaCompleted>();
        services.AddEventType<SagaCompensated>();
        services.AddEventType<SagaFailed>();
        services.AddCommandHandler<StartSagaCommand<TInput>, TSaga, StartSagaCommandHandler<TSaga, TInput>>();
        services.AddReducer<SagaStartedEvent, TSaga, SagaStartedReducer<TSaga>>();
        services.AddReducer<SagaStepCompleted, TSaga, SagaStepCompletedReducer<TSaga>>();
        services.AddReducer<SagaCompensating, TSaga, SagaCompensatingReducer<TSaga>>();
        services.AddReducer<SagaCompleted, TSaga, SagaCompletedReducer<TSaga>>();
        services.AddReducer<SagaCompensated, TSaga, SagaCompensatedReducer<TSaga>>();
        services.AddReducer<SagaFailed, TSaga, SagaFailedReducer<TSaga>>();
        services.AddRootEventEffect<TSaga>();
        services.AddTransient<IEventEffect<TSaga>, SagaOrchestrationEffect<TSaga>>();
        return services;
    }
}
