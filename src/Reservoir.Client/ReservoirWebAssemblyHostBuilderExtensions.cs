using System;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;


namespace Mississippi.Reservoir.Client;

/// <summary>
///     Host-builder extensions for attaching Reservoir to Blazor WebAssembly applications.
/// </summary>
public static class ReservoirWebAssemblyHostBuilderExtensions
{
    private const string ServicesPropertyName = "Services";

    private const string SupportedBuilderTypeName =
        "Microsoft.AspNetCore.Components.WebAssembly.Hosting.WebAssemblyHostBuilder";

    /// <summary>
    ///     Attaches Reservoir to the Blazor WebAssembly host builder.
    /// </summary>
    /// <typeparam name="TBuilder">The host-builder type.</typeparam>
    /// <param name="builder">The WebAssembly host builder.</param>
    /// <param name="configure">The Reservoir configuration callback.</param>
    /// <returns>The same host builder for chaining.</returns>
    public static TBuilder AddReservoir<TBuilder>(
        this TBuilder builder,
        Action<IReservoirBuilder> configure
    )
        where TBuilder : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        ValidateSupportedBuilder(builder);
        IServiceCollection services = GetServices(builder);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(ReservoirHostAttachMarker)))
        {
            throw new ReservoirBuilderValidationException(
                "Reservoir was attached to this WebAssemblyHostBuilder more than once.");
        }

        ReservoirBuilder reservoirBuilder = new(services);
        configure(reservoirBuilder);
        reservoirBuilder.Validate();
        services.AddSingleton(ReservoirHostAttachMarker.Instance);
        return builder;
    }

    private static IServiceCollection GetServices<TBuilder>(
        TBuilder builder
    )
        where TBuilder : class
    {
        PropertyInfo? servicesProperty = builder.GetType()
            .GetProperty(ServicesPropertyName, BindingFlags.Instance | BindingFlags.Public);
        if (servicesProperty?.GetValue(builder) is IServiceCollection services)
        {
            return services;
        }

        throw new ReservoirBuilderValidationException(
            $"Reservoir requires a supported host builder with a public {ServicesPropertyName} property of type {nameof(IServiceCollection)}. Builder type: {builder.GetType().FullName}.");
    }

    private static void ValidateSupportedBuilder<TBuilder>(
        TBuilder builder
    )
        where TBuilder : class
    {
        if (!string.Equals(builder.GetType().FullName, SupportedBuilderTypeName, StringComparison.Ordinal))
        {
            throw new ReservoirBuilderValidationException(
                $"Reservoir currently supports only {SupportedBuilderTypeName} for host attach. Builder type: {builder.GetType().FullName}.");
        }
    }

    private sealed class ReservoirHostAttachMarker
    {
        public static ReservoirHostAttachMarker Instance { get; } = new();
    }
}