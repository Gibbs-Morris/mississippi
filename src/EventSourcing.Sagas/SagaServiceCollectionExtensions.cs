using System;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Extension methods for registering saga services.
/// </summary>
public static class SagaServiceCollectionExtensions
{
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

    /// <summary>
    ///     Registers saga steps and compensations for a saga type.
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
        // The actual step/compensation types are discovered at runtime from the
        // same assembly as TSaga based on SagaStepAttribute and SagaCompensationAttribute.
        RegisterStepsAndCompensations<TSaga>(services);

        return services;
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

        foreach (Type stepType in sagaAssembly
                     .GetTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false } && stepBaseType.IsAssignableFrom(t))
                     .Where(t => t.GetCustomAttribute<SagaStepAttribute>() is not null))
        {
            services.TryAddTransient(stepType);
        }

        // Find and register all compensation types that extend SagaCompensationBase<TSaga>
        Type compensationBaseType = typeof(SagaCompensationBase<TSaga>);

        foreach (Type compensationType in sagaAssembly
                     .GetTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false } && compensationBaseType.IsAssignableFrom(t))
                     .Where(t => t.GetCustomAttribute<SagaCompensationAttribute>() is not null))
        {
            services.TryAddTransient(compensationType);
        }
    }
}
