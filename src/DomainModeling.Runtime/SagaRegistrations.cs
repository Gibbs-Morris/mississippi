using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Runtime;


namespace Mississippi.DomainModeling.Runtime;

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
        services.AddOptions<SagaRecoveryOptions>();
        services.AddSnapshotType<SagaRecoveryCheckpoint>();
        services.AddSnapshotStateConverter<SagaRecoveryCheckpoint>();
        services.AddEventType<SagaStartedEvent>();
        services.AddEventType<SagaInputProvided<TInput>>();
        services.AddEventType<SagaStepExecutionStarted>();
        services.AddEventType<SagaResumeBlocked>();
        services.AddEventType<SagaStepCompleted>();
        services.AddEventType<SagaStepFailed>();
        services.AddEventType<SagaCompensating>();
        services.AddEventType<SagaStepCompensated>();
        services.AddEventType<SagaCompleted>();
        services.AddEventType<SagaCompensated>();
        services.AddEventType<SagaFailed>();
        services.TryAddSingleton<ISagaRecoveryInfoProvider<TSaga>>(
            new SagaRecoveryInfoProvider<TSaga>(
                new(
                    SagaRecoveryMode.Automatic,
                    null)));
        services.AddCommandHandler<StartSagaCommand<TInput>, TSaga, StartSagaCommandHandler<TSaga, TInput>>();
        services.AddCommandHandler<ResumeSagaCommand, TSaga, ResumeSagaCommandHandler<TSaga>>();
        services.AddReducer<SagaStartedEvent, TSaga, SagaStartedReducer<TSaga>>();
        services.AddReducer<SagaInputProvided<TInput>, TSaga, SagaInputProvidedReducer<TSaga, TInput>>();
        services.AddReducer<SagaStepCompleted, TSaga, SagaStepCompletedReducer<TSaga>>();
        services.AddReducer<SagaCompensating, TSaga, SagaCompensatingReducer<TSaga>>();
        services.AddReducer<SagaCompleted, TSaga, SagaCompletedReducer<TSaga>>();
        services.AddReducer<SagaCompensated, TSaga, SagaCompensatedReducer<TSaga>>();
        services.AddReducer<SagaFailed, TSaga, SagaFailedReducer<TSaga>>();
        services.AddReducer<SagaStartedEvent, SagaRecoveryCheckpoint, SagaRecoveryCheckpointStartedReducer>();
        services.AddReducer<
            SagaStepExecutionStarted,
            SagaRecoveryCheckpoint,
            SagaRecoveryCheckpointExecutionStartedReducer>();
        services.AddReducer<SagaResumeBlocked, SagaRecoveryCheckpoint, SagaRecoveryCheckpointResumeBlockedReducer>();
        services.AddReducer<SagaStepCompleted, SagaRecoveryCheckpoint, SagaRecoveryCheckpointStepCompletedReducer>();
        services.AddReducer<SagaStepFailed, SagaRecoveryCheckpoint, SagaRecoveryCheckpointStepFailedReducer>();
        services.AddReducer<SagaCompensating, SagaRecoveryCheckpoint, SagaRecoveryCheckpointCompensatingReducer>();
        services.AddReducer<
            SagaStepCompensated,
            SagaRecoveryCheckpoint,
            SagaRecoveryCheckpointStepCompensatedReducer>();
        services.AddReducer<SagaCompleted, SagaRecoveryCheckpoint, SagaRecoveryCheckpointCompletedReducer>();
        services.AddReducer<SagaCompensated, SagaRecoveryCheckpoint, SagaRecoveryCheckpointCompensatedReducer>();
        services.AddReducer<SagaFailed, SagaRecoveryCheckpoint, SagaRecoveryCheckpointFailedReducer>();
        services.AddTransient<SagaRecoveryCheckpointAccessor<TSaga>>();
        services.AddTransient<SagaRecoveryCoordinator<TSaga>>();
        services.AddTransient<SagaRecoveryPlanner<TSaga>>();
        services.TryAddSingleton<IGrainReminderManager, GrainReminderManager>();
        services.TryAddTransient<IAggregateReminderHandler<TSaga>, SagaReminderHandler<TSaga>>();
        services.TryAddTransient<IAggregateReminderReconciler<TSaga>, SagaReminderReconciler<TSaga>>();
        services.AddRootEventEffect<TSaga>();
        services.AddTransient<IEventEffect<TSaga>, SagaOrchestrationEffect<TSaga>>();
        return services;
    }
}