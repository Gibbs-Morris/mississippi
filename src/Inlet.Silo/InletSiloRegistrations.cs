using System;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Silo.Abstractions;


namespace Mississippi.Inlet.Silo;

/// <summary>
///     Extension methods for registering Inlet Silo services.
/// </summary>
/// <remarks>
///     <para>
///         Use these extensions on Orleans silo hosts. For ASP.NET Core hosts
///         that serve SignalR hubs, use the extensions from <c>Inlet.Server</c>.
///     </para>
/// </remarks>
public static class InletSiloRegistrations
{
    /// <summary>
    ///     Adds Inlet Orleans services to the Mississippi silo builder.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers the <see cref="IProjectionBrookRegistry" /> as a singleton.
    ///         You must call <see cref="ScanProjectionAssemblies(IMississippiSiloBuilder, Assembly[])" /> to populate the
    ///         registry
    ///         with projection-to-brook mappings from attributed types.
    ///     </para>
    /// </remarks>
    public static IMississippiSiloBuilder AddInletSilo(
        this IMississippiSiloBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
            services.TryAddSingleton<IProjectionBrookRegistry, ProjectionBrookRegistry>());
        return builder;
    }

    /// <summary>
    ///     Adds Inlet Orleans services to the Mississippi server builder.
    /// </summary>
    /// <param name="builder">The Mississippi server builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiServerBuilder AddInletSilo(
        this IMississippiServerBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
            services.TryAddSingleton<IProjectionBrookRegistry, ProjectionBrookRegistry>());
        return builder;
    }

    /// <summary>
    ///     Scans assemblies for projection types and registers them in the brook registry.
    /// </summary>
    /// <param name="builder">The Mississippi silo builder.</param>
    /// <param name="assemblies">The assemblies to scan for projection types.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method scans the provided assemblies for types decorated with
    ///         <see cref="ProjectionPathAttribute" /> and registers their path-to-brook
    ///         mappings in the <see cref="IProjectionBrookRegistry" />.
    ///     </para>
    ///     <para>
    ///         The brook name is determined from <see cref="BrookNameAttribute" /> if present,
    ///         otherwise defaults to the path from <see cref="ProjectionPathAttribute" />.
    ///     </para>
    ///     <para>
    ///         Call this after <see cref="AddInletSilo(IMississippiSiloBuilder)" /> to populate the registry.
    ///     </para>
    /// </remarks>
    public static IMississippiSiloBuilder ScanProjectionAssemblies(
        this IMississippiSiloBuilder builder,
        params Assembly[] assemblies
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(assemblies);
        ProjectionBrookRegistry registry = BuildProjectionBrookRegistry(assemblies);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IProjectionBrookRegistry>();
            services.AddSingleton<IProjectionBrookRegistry>(registry);
        });
        return builder;
    }

    /// <summary>
    ///     Scans assemblies for projection types and registers them in the brook registry.
    /// </summary>
    /// <param name="builder">The Mississippi server builder.</param>
    /// <param name="assemblies">The assemblies to scan for projection types.</param>
    /// <returns>The builder for chaining.</returns>
    public static IMississippiServerBuilder ScanProjectionAssemblies(
        this IMississippiServerBuilder builder,
        params Assembly[] assemblies
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(assemblies);
        ProjectionBrookRegistry registry = BuildProjectionBrookRegistry(assemblies);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IProjectionBrookRegistry>();
            services.AddSingleton<IProjectionBrookRegistry>(registry);
        });
        return builder;
    }

    private static ProjectionBrookRegistry BuildProjectionBrookRegistry(
        Assembly[] assemblies
    )
    {
        ProjectionBrookRegistry registry = new();
        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in assembly.GetExportedTypes())
            {
                ProjectionPathAttribute? pathAttr = type.GetCustomAttribute<ProjectionPathAttribute>();
                if (pathAttr is null)
                {
                    continue;
                }

                // Brook name from BrookNameAttribute, or default to path
                BrookNameAttribute? brookAttr = type.GetCustomAttribute<BrookNameAttribute>();
                string brookName = brookAttr?.BrookName ?? pathAttr.Path;
                registry.Register(pathAttr.Path, brookName);
            }
        }

        return registry;
    }
}