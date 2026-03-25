using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace Mississippi.Hosting.Runtime;

/// <summary>
///     Extension methods for composing Mississippi runtime applications from a host application builder.
/// </summary>
public static class MississippiRuntimeHostBuilderRegistrations
{
    /// <summary>
    ///     Adds the top-level Mississippi runtime builder to a host application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The Mississippi runtime builder for further runtime composition.</returns>
    public static MississippiRuntimeBuilder AddMississippiRuntime(
        this IHostApplicationBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        MississippiRuntimeConfigurationTrustGuards.ThrowIfUnsafeConfigurationExists(
            builder.Configuration,
            builder.Environment.EnvironmentName);
        MississippiRuntimeCompositionGuards.ThrowIfUnsupportedCompositionExists(builder.Services);
        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(MississippiRuntimeBuilderState)))
        {
            throw new InvalidOperationException(
                "AddMississippiRuntime can only be called once per host. Compose additional runtime configuration through the existing MississippiRuntimeBuilder instance.");
        }

        MississippiRuntimeBuilderState state = new(builder.Services, builder.Configuration, builder.Environment);
        builder.Services.AddSingleton(MississippiRuntimeHostModeMarker.Instance);
        builder.Services.AddSingleton(state);
        builder.UseOrleans(state.ApplyOrleansConfiguration);
        return new(builder.Services, state);
    }

    /// <summary>
    ///     Adds the top-level Mississippi runtime builder to a host application builder and invokes a configuration callback.
    /// </summary>
    /// <typeparam name="TBuilder">The native host builder type.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">The runtime composition callback.</param>
    /// <returns>The original native host builder.</returns>
    public static TBuilder AddMississippiRuntime<TBuilder>(
        this TBuilder builder,
        Action<MississippiRuntimeBuilder> configure
    )
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        configure(builder.AddMississippiRuntime());
        return builder;
    }
}