using System;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Provides extension methods for registering saga recovery metadata.
/// </summary>
public static class SagaRecoveryInfoRegistrations
{
    /// <summary>
    ///     Registers saga recovery metadata for the provided saga state type.
    /// </summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="recovery">The saga recovery metadata.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSagaRecoveryInfo<TSaga>(
        this IServiceCollection services,
        SagaRecoveryInfo recovery
    )
        where TSaga : class, ISagaState
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(recovery);
        services.AddSingleton<ISagaRecoveryInfoProvider<TSaga>>(new SagaRecoveryInfoProvider<TSaga>(recovery));
        return services;
    }
}
