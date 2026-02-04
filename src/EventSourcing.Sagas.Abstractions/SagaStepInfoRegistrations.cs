using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Provides extension methods for registering saga step metadata.
/// </summary>
public static class SagaStepInfoRegistrations
{
    /// <summary>
    ///     Registers saga step metadata for the provided saga state type.
    /// </summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="steps">The ordered saga step metadata.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSagaStepInfo<TSaga>(
        this IServiceCollection services,
        IReadOnlyList<SagaStepInfo> steps
    )
        where TSaga : class, ISagaState
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(steps);
        services.AddSingleton<ISagaStepInfoProvider<TSaga>>(new SagaStepInfoProvider<TSaga>(steps));
        return services;
    }
}