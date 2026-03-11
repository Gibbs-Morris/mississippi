#pragma warning disable S1133 // Intentional staged deprecation pending issue #237.
using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Provides extension methods for registering saga step metadata.
/// </summary>
[Obsolete(
    "Legacy runtime composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to RuntimeBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
    false)]
public static class SagaStepInfoRegistrations
{
    /// <summary>
    ///     Registers saga step metadata for the provided saga state type.
    /// </summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="steps">The ordered saga step metadata.</param>
    /// <returns>The updated service collection.</returns>
    [Obsolete(
        "Legacy runtime composition entrypoint. Will be removed once GitHub issue #237 (Host/Sub-Builder Composition Model) is fully implemented. Migrate to RuntimeBuilder via UseMississippi() once available (see issue #237, in progress). See: https://github.com/Gibbs-Morris/mississippi/issues/237",
        false)]
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

#pragma warning restore S1133