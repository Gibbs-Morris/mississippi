using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Abstractions;
using Mississippi.Common.Builders.Runtime.Abstractions;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime;

/// <summary>
///     Runtime-builder extension methods for Tributary infrastructure registration.
/// </summary>
public static class TributaryRuntimeBuilderExtensions
{
    /// <summary>
    ///     Adds Tributary runtime infrastructure to the runtime builder.
    /// </summary>
    /// <param name="builder">Runtime builder.</param>
    /// <param name="configure">Optional Tributary runtime configuration.</param>
    /// <returns>The same runtime builder instance for fluent chaining.</returns>
    public static IRuntimeBuilder AddTributary(
        this IRuntimeBuilder builder,
        Action<ITributaryRuntimeBuilder>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        TributaryRuntimeBuilder tributaryRuntimeBuilder = new(builder.Services);
        configure?.Invoke(tributaryRuntimeBuilder);
        return builder;
    }

    /// <summary>
    ///     Enables in-memory snapshot caching.
    /// </summary>
    /// <param name="builder">Tributary runtime sub-builder.</param>
    /// <returns>The same Tributary runtime sub-builder for fluent chaining.</returns>
    public static ITributaryRuntimeBuilder EnableSnapshotCaching(
        this ITributaryRuntimeBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSnapshotCaching();
        return builder;
    }

    private sealed class TributaryRuntimeBuilder : ITributaryRuntimeBuilder
    {
        public TributaryRuntimeBuilder(
            IServiceCollection services
        ) =>
            Services = services;

        public IServiceCollection Services { get; }

        /// <inheritdoc />
        public IReadOnlyList<BuilderDiagnostic> Validate() => [];
    }
}