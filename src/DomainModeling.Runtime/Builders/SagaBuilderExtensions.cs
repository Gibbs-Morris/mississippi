using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Brooks.Abstractions;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Abstractions.Builders;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime;


namespace Mississippi.DomainModeling.Runtime.Builders;

/// <summary>
///     Extension methods for registering saga components through the builder model.
/// </summary>
public static class SagaBuilderExtensions
{
    /// <summary>
    ///     Registers an event reducer for saga state computation.
    /// </summary>
    /// <typeparam name="TEvent">The event type consumed by the reducer.</typeparam>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <typeparam name="TReducer">The reducer implementation type.</typeparam>
    /// <param name="builder">The saga builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static ISagaBuilder AddReducer<TEvent, TSaga, TReducer>(
        this ISagaBuilder builder
    )
        where TReducer : class, IEventReducer<TEvent, TSaga>
    {
        IServiceCollection services = CastBuilder(builder).Services;
        services.AddTransient<IEventReducer<TSaga>, TReducer>();
        services.AddTransient<IEventReducer<TEvent, TSaga>, TReducer>();
        services.TryAddTransient<IRootReducer<TSaga>, RootReducer<TSaga>>();
        return builder;
    }

    /// <summary>
    ///     Registers saga orchestration infrastructure for the specified saga and input types.
    /// </summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <typeparam name="TInput">The saga input type.</typeparam>
    /// <param name="builder">The saga builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static ISagaBuilder AddSagaOrchestration<TSaga, TInput>(
        this ISagaBuilder builder
    )
        where TSaga : class, ISagaState
    {
        SagaBuilder impl = CastBuilder(builder);
        impl.EnsureNotDuplicate<TSaga>();
        IServiceCollection services = impl.Services;

        // Register aggregate infrastructure (event/snapshot type registries, BrookEventConverter, etc.)
        services.TryAddSingleton<IEventTypeRegistry>(provider =>
        {
            EventTypeRegistry registry = new();
            foreach (IEventTypeRegistration registration in provider.GetServices<IEventTypeRegistration>())
            {
                registration.Register(registry);
            }

            return registry;
        });
        services.TryAddSingleton<ISnapshotTypeRegistry>(provider =>
        {
            SnapshotTypeRegistry registry = new();
            foreach (ISnapshotTypeRegistration registration in provider.GetServices<ISnapshotTypeRegistration>())
            {
                registration.Register(registry);
            }

            return registry;
        });
        services.TryAddTransient<IBrookEventConverter, BrookEventConverter>();
        services.TryAddTransient<IAggregateGrainFactory, AggregateGrainFactory>();
        services.TryAddSingleton(TimeProvider.System);

        // Register saga event types
        services.AddSingleton<IEventTypeRegistration>(new EventTypeRegistration<SagaStartedEvent>());
        services.AddSingleton<IEventTypeRegistration>(new EventTypeRegistration<SagaInputProvided<TInput>>());
        services.AddSingleton<IEventTypeRegistration>(new EventTypeRegistration<SagaStepCompleted>());
        services.AddSingleton<IEventTypeRegistration>(new EventTypeRegistration<SagaStepFailed>());
        services.AddSingleton<IEventTypeRegistration>(new EventTypeRegistration<SagaCompensating>());
        services.AddSingleton<IEventTypeRegistration>(new EventTypeRegistration<SagaStepCompensated>());
        services.AddSingleton<IEventTypeRegistration>(new EventTypeRegistration<SagaCompleted>());
        services.AddSingleton<IEventTypeRegistration>(new EventTypeRegistration<SagaCompensated>());
        services.AddSingleton<IEventTypeRegistration>(new EventTypeRegistration<SagaFailed>());

        // Register saga command handler
        services.AddTransient<ICommandHandler<TSaga>, StartSagaCommandHandler<TSaga, TInput>>();
        services.AddTransient<ICommandHandler<StartSagaCommand<TInput>, TSaga>,
            StartSagaCommandHandler<TSaga, TInput>>();
        services.TryAddTransient<IRootCommandHandler<TSaga>, RootCommandHandler<TSaga>>();

        // Register saga reducers
        services.AddTransient<IEventReducer<TSaga>, SagaStartedReducer<TSaga>>();
        services.AddTransient<IEventReducer<SagaStartedEvent, TSaga>, SagaStartedReducer<TSaga>>();
        services.AddTransient<IEventReducer<TSaga>, SagaInputProvidedReducer<TSaga, TInput>>();
        services.AddTransient<IEventReducer<SagaInputProvided<TInput>, TSaga>,
            SagaInputProvidedReducer<TSaga, TInput>>();
        services.AddTransient<IEventReducer<TSaga>, SagaStepCompletedReducer<TSaga>>();
        services.AddTransient<IEventReducer<SagaStepCompleted, TSaga>, SagaStepCompletedReducer<TSaga>>();
        services.AddTransient<IEventReducer<TSaga>, SagaCompensatingReducer<TSaga>>();
        services.AddTransient<IEventReducer<SagaCompensating, TSaga>, SagaCompensatingReducer<TSaga>>();
        services.AddTransient<IEventReducer<TSaga>, SagaCompletedReducer<TSaga>>();
        services.AddTransient<IEventReducer<SagaCompleted, TSaga>, SagaCompletedReducer<TSaga>>();
        services.AddTransient<IEventReducer<TSaga>, SagaCompensatedReducer<TSaga>>();
        services.AddTransient<IEventReducer<SagaCompensated, TSaga>, SagaCompensatedReducer<TSaga>>();
        services.AddTransient<IEventReducer<TSaga>, SagaFailedReducer<TSaga>>();
        services.AddTransient<IEventReducer<SagaFailed, TSaga>, SagaFailedReducer<TSaga>>();
        services.TryAddTransient<IRootReducer<TSaga>, RootReducer<TSaga>>();

        // Register saga event effect and root effect
        services.TryAddTransient<IRootEventEffect<TSaga>, RootEventEffect<TSaga>>();
        services.AddTransient<IEventEffect<TSaga>, SagaOrchestrationEffect<TSaga>>();
        return builder;
    }

    /// <summary>
    ///     Registers a saga step as a transient service.
    /// </summary>
    /// <typeparam name="TStep">The saga step type.</typeparam>
    /// <param name="builder">The saga builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static ISagaBuilder AddSagaStep<TStep>(
        this ISagaBuilder builder
    )
        where TStep : class
    {
        IServiceCollection services = CastBuilder(builder).Services;
        services.AddTransient<TStep>();
        return builder;
    }

    /// <summary>
    ///     Registers saga step info metadata for ordered step execution.
    /// </summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="builder">The saga builder.</param>
    /// <param name="steps">The ordered saga step metadata.</param>
    /// <returns>The builder for chaining.</returns>
    public static ISagaBuilder AddSagaStepInfo<TSaga>(
        this ISagaBuilder builder,
        IReadOnlyList<SagaStepInfo> steps
    )
        where TSaga : class, ISagaState
    {
        IServiceCollection services = CastBuilder(builder).Services;
        services.AddSingleton<ISagaStepInfoProvider<TSaga>>(new SagaStepInfoProvider<TSaga>(steps));
        return builder;
    }

    /// <summary>
    ///     Registers a snapshot state converter for periodic saga state snapshots.
    /// </summary>
    /// <typeparam name="TSnapshot">The saga state type to convert.</typeparam>
    /// <param name="builder">The saga builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static ISagaBuilder AddSnapshotStateConverter<TSnapshot>(
        this ISagaBuilder builder
    )
    {
        IServiceCollection services = CastBuilder(builder).Services;
        services.TryAddTransient<ISnapshotStateConverter<TSnapshot>, SnapshotStateConverter<TSnapshot>>();
        return builder;
    }

    private static SagaBuilder CastBuilder(
        ISagaBuilder builder
    ) =>
        builder as SagaBuilder ??
        throw new InvalidOperationException(
            $"Expected {nameof(SagaBuilder)} but received {builder.GetType().FullName}.");
}