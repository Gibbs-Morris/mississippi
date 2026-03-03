using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Common.Builders.Abstractions;
using Mississippi.Common.Builders.Runtime.Abstractions;


namespace Mississippi.Brooks.Runtime;

/// <summary>
///     Runtime-builder extension methods for Brooks infrastructure registration.
/// </summary>
public static class BrooksRuntimeBuilderExtensions
{
    /// <summary>
    ///     Adds Brooks runtime infrastructure to the runtime builder.
    /// </summary>
    /// <param name="builder">Runtime builder.</param>
    /// <param name="configure">Optional Brooks runtime configuration.</param>
    /// <returns>The same runtime builder instance for fluent chaining.</returns>
    public static IRuntimeBuilder AddBrooks(
        this IRuntimeBuilder builder,
        Action<IBrooksRuntimeBuilder>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddEventSourcingByService();
        BrooksRuntimeBuilder brooksRuntimeBuilder = new(builder);
        configure?.Invoke(brooksRuntimeBuilder);
        builder.MarkFeatureConfigured("Runtime.Brooks");
        return builder;
    }

    private sealed class BrooksRuntimeBuilder : IBrooksRuntimeBuilder
    {
        public BrooksRuntimeBuilder(
            IRuntimeBuilder runtimeBuilder
        ) =>
            RuntimeBuilder = runtimeBuilder;

        public IServiceCollection Services => RuntimeBuilder.Services;

        private IRuntimeBuilder RuntimeBuilder { get; }

        public IBrooksRuntimeBuilder ConfigureStreaming(
            Action<BrookProviderOptions> configure
        )
        {
            ArgumentNullException.ThrowIfNull(configure);
            RuntimeBuilder.AddSiloConfiguration(siloBuilder => siloBuilder.AddEventSourcing(configure));
            return this;
        }

        /// <inheritdoc />
        public IReadOnlyList<BuilderDiagnostic> Validate() => [];
    }
}