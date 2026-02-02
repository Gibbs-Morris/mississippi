using System;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Commands;
using Mississippi.EventSourcing.Sagas.Effects;


namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Extension methods for registering saga services.
/// </summary>
public static class SagaServiceCollectionExtensions
{
    /// <summary>
    ///     Registers saga steps, compensations, and effects for a saga type.
    /// </summary>
    /// <typeparam name="TSaga">The saga state type implementing <see cref="ISagaDefinition" />.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSaga<TSaga>(
        this IServiceCollection services
    )
        where TSaga : class, ISagaDefinition
    {
        // Access static property to ensure TSaga is used (avoids S2326)
        _ = TSaga.SagaName;

        // Register the step registry for this saga type
        services.TryAddSingleton<ISagaStepRegistry<TSaga>, SagaStepRegistry<TSaga>>();

        // Register saga steps and compensations discovered via reflection.
        RegisterStepsAndCompensations<TSaga>(services);

        // Register saga event effects for step orchestration
        RegisterSagaEffects<TSaga>(services);
        return services;
    }

    /// <summary>
    ///     Registers saga with a specific input type for command handling.
    /// </summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <typeparam name="TInput">The input type for starting the saga.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSaga<TSaga, TInput>(
        this IServiceCollection services
    )
        where TSaga : class, ISagaDefinition
        where TInput : class
    {
        // Register base saga services
        services.AddSaga<TSaga>();

        // Register the start saga command handler
        services.AddCommandHandler<
            StartSagaCommand<TInput>,
            TSaga,
            StartSagaCommandHandler<TInput, TSaga>>();
        return services;
    }

    /// <summary>
    ///     Adds saga orchestration services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSagaOrchestration(
        this IServiceCollection services
    )
    {
        services.TryAddSingleton<ISagaOrchestrator, SagaOrchestrator>();
        return services;
    }

    private static void RegisterSagaEffects<TSaga>(
        IServiceCollection services
    )
        where TSaga : class
    {
        // Register step started effect - executes steps
        services.TryAddTransient<IEventEffect<TSaga>, SagaStepStartedEffect<TSaga>>();

        // Register step completed effect - progresses to next step
        services.TryAddTransient<IEventEffect<TSaga>, SagaStepCompletedEffect<TSaga>>();

        // Register step failed effect - triggers compensation
        services.TryAddTransient<IEventEffect<TSaga>, SagaStepFailedEffect<TSaga>>();
    }

    private static void RegisterStepsAndCompensations<TSaga>(
        IServiceCollection services
    )
        where TSaga : class
    {
        Type sagaType = typeof(TSaga);
        Assembly sagaAssembly = sagaType.Assembly;

        // Find and register all step types that extend SagaStepBase<TSaga>
        Type stepBaseType = typeof(SagaStepBase<TSaga>);
        foreach (Type stepType in sagaAssembly.GetTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false } && stepBaseType.IsAssignableFrom(t))
                     .Where(t => t.GetCustomAttribute<SagaStepAttribute>() is not null))
        {
            services.TryAddTransient(stepType);
        }

        // Find and register all compensation types that extend SagaCompensationBase<TSaga>
        Type compensationBaseType = typeof(SagaCompensationBase<TSaga>);
        foreach (Type compensationType in sagaAssembly.GetTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false } && compensationBaseType.IsAssignableFrom(t))
                     .Where(t => t.GetCustomAttribute<SagaCompensationAttribute>() is not null))
        {
            services.TryAddTransient(compensationType);
        }
    }
}