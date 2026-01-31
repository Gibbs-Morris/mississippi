using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.Abstractions.State;
using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Reservoir;


namespace Mississippi.Inlet.Client;

/// <summary>
///     Builder for configuring Inlet Blazor SignalR services.
/// </summary>
public sealed class InletBlazorSignalRBuilder
{
    private readonly List<Assembly> assembliesToScan = [];

    private readonly List<Action<IProjectionDtoRegistry>> registryConfigurations = [];

    private InletSignalRActionEffectOptions options = new();

    private Type? projectionFetcherType;

    private string? routePrefix;

    private bool useAutoFetcher;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InletBlazorSignalRBuilder" /> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public InletBlazorSignalRBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    private IServiceCollection Services { get; }

    /// <summary>
    ///     Registers a projection fetcher implementation.
    /// </summary>
    /// <typeparam name="TFetcher">The projection fetcher type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public InletBlazorSignalRBuilder AddProjectionFetcher<TFetcher>()
        where TFetcher : class, IProjectionFetcher
    {
        projectionFetcherType = typeof(TFetcher);
        useAutoFetcher = false;
        return this;
    }

    /// <summary>
    ///     Registers projection DTO types explicitly using a configuration action.
    /// </summary>
    /// <param name="configure">An action that configures the projection DTO registry.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method enables the <see cref="AutoProjectionFetcher" /> without requiring
    ///         runtime assembly scanning. Use this for compile-time known registrations,
    ///         typically from source generators.
    ///     </para>
    ///     <code>
    ///         builder.RegisterProjectionDtos(registry =&gt;
    ///         {
    ///             registry.Register("accounts/balance", typeof(BankAccountBalanceProjectionDto));
    ///         });
    ///     </code>
    /// </remarks>
    public InletBlazorSignalRBuilder RegisterProjectionDtos(
        Action<IProjectionDtoRegistry> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        registryConfigurations.Add(configure);
        useAutoFetcher = true;
        return this;
    }

    /// <summary>
    ///     Scans the specified assemblies for types decorated with
    ///     <see cref="ProjectionPathAttribute" /> and registers them in the
    ///     <see cref="IProjectionDtoRegistry" />.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method enables the <see cref="AutoProjectionFetcher" /> which uses
    ///         the registry to automatically fetch projections based on DTO type.
    ///     </para>
    /// </remarks>
    public InletBlazorSignalRBuilder ScanProjectionDtos(
        params Assembly[] assemblies
    )
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        assembliesToScan.AddRange(assemblies);
        useAutoFetcher = true;
        return this;
    }

    /// <summary>
    ///     Sets the SignalR hub path.
    /// </summary>
    /// <param name="hubPath">The hub path (e.g., "/hubs/inlet").</param>
    /// <returns>The builder for chaining.</returns>
    public InletBlazorSignalRBuilder WithHubPath(
        string hubPath
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hubPath);
        options = new()
        {
            HubPath = hubPath,
        };
        return this;
    }

    /// <summary>
    ///     Sets the route prefix for projection endpoints.
    /// </summary>
    /// <param name="prefix">The route prefix (e.g., "/api/projections").</param>
    /// <returns>The builder for chaining.</returns>
    public InletBlazorSignalRBuilder WithRoutePrefix(
        string prefix
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        routePrefix = prefix;
        return this;
    }

    /// <summary>
    ///     Builds and registers all configured services.
    /// </summary>
    internal void Build()
    {
        // Capture configuration lists for the factory closure
        List<Assembly> scanAssemblies = [.. assembliesToScan];
        List<Action<IProjectionDtoRegistry>> configurations = [.. registryConfigurations];

        // Register options
        Services.TryAddSingleton(options);

        // Register the projection DTO registry (always available)
        Services.TryAddSingleton<IProjectionDtoRegistry>(sp =>
        {
            ProjectionDtoRegistry registry = new();

            // Apply assembly scanning (runtime reflection)
            foreach (Assembly assembly in scanAssemblies)
            {
                registry.ScanAssemblies(assembly);
            }

            // Apply explicit configurations (compile-time registrations from generators)
            foreach (Action<IProjectionDtoRegistry> configure in configurations)
            {
                configure(registry);
            }

            return registry;
        });

        // Register the projection fetcher with scoped lifetime to match Store/Effect pattern (Fluxor style).
        // In Blazor WASM, scoped = singleton. In Blazor Server, each circuit gets its own fetcher.
        if (useAutoFetcher)
        {
            // Use the auto fetcher with registry
            Services.TryAddScoped<IProjectionFetcher>(sp =>
            {
                HttpClient http = sp.GetRequiredService<HttpClient>();
                IProjectionDtoRegistry registry = sp.GetRequiredService<IProjectionDtoRegistry>();
                return new AutoProjectionFetcher(http, registry, routePrefix);
            });
        }
        else if (projectionFetcherType is not null)
        {
            // Use custom fetcher
            Services.TryAddScoped(typeof(IProjectionFetcher), projectionFetcherType);
        }

        // Register the hub connection provider
        Services.TryAddScoped<IHubConnectionProvider, HubConnectionProvider>();

        // Register Lazy<IInletStore> to break the circular dependency:
        // Store resolves IActionEffect[] → InletSignalRActionEffect needs Store.
        // By using Lazy<IInletStore>, the effect defers resolution until first use.
        Services.TryAddScoped<Lazy<IInletStore>>(sp => new(() => sp.GetRequiredService<IInletStore>()));

        // Register the Inlet connection feature state and SignalR effect
        Services.AddActionEffect<InletConnectionState, InletSignalRActionEffect>();

        // Register the SignalR connection feature (state, reducers, and lifecycle effect)
        Services.AddSignalRConnectionFeature();
    }
}