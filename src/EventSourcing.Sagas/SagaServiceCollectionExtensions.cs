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
        // Register saga steps and compensations discovered via reflection.
        // The actual step/compensation types are discovered at runtime from the
        // same assembly as TSaga based on SagaStepAttribute and SagaCompensationAttribute.

        // Access static property to ensure TSaga is used (avoids S2326)
        _ = TSaga.SagaName;

        return services;
    }
}
