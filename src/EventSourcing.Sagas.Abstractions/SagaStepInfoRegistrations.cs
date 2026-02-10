using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;


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
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <param name="steps">The ordered saga step metadata.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiSiloBuilder AddSagaStepInfo<TSaga>(
        this IMississippiSiloBuilder builder,
        IReadOnlyList<SagaStepInfo> steps
    )
        where TSaga : class, ISagaState
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(steps);
        builder.ConfigureServices(services =>
            services.AddSingleton<ISagaStepInfoProvider<TSaga>>(new SagaStepInfoProvider<TSaga>(steps)));
        return builder;
    }
}