using System;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.EventSourcing.Aggregates;
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
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddSagaOrchestration<TSaga, TInput>(
        this IMississippiSiloBuilder builder
    )
        where TSaga : class, ISagaState
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddAggregateSupport();
        builder.AddEventType<SagaStartedEvent>();
        builder.AddEventType<SagaInputProvided<TInput>>();
        builder.AddEventType<SagaStepCompleted>();
        builder.AddEventType<SagaStepFailed>();
        builder.AddEventType<SagaCompensating>();
        builder.AddEventType<SagaStepCompensated>();
        builder.AddEventType<SagaCompleted>();
        builder.AddEventType<SagaCompensated>();
        builder.AddEventType<SagaFailed>();
        builder.AddCommandHandler<StartSagaCommand<TInput>, TSaga, StartSagaCommandHandler<TSaga, TInput>>();
        builder.AddReducer<SagaStartedEvent, TSaga, SagaStartedReducer<TSaga>>();
        builder.AddReducer<SagaInputProvided<TInput>, TSaga, SagaInputProvidedReducer<TSaga, TInput>>();
        builder.AddReducer<SagaStepCompleted, TSaga, SagaStepCompletedReducer<TSaga>>();
        builder.AddReducer<SagaCompensating, TSaga, SagaCompensatingReducer<TSaga>>();
        builder.AddReducer<SagaCompleted, TSaga, SagaCompletedReducer<TSaga>>();
        builder.AddReducer<SagaCompensated, TSaga, SagaCompensatedReducer<TSaga>>();
        builder.AddReducer<SagaFailed, TSaga, SagaFailedReducer<TSaga>>();
        builder.AddEventEffect<SagaOrchestrationEffect<TSaga>, TSaga>();
        return builder;
    }
}