using System;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.EventSourcing.UxProjections.Abstractions.Attributes;
using Mississippi.Inlet.Abstractions;


namespace Mississippi.Inlet.Orleans;

/// <summary>
///     Extension methods for registering Inlet Orleans services.
/// </summary>
/// <remarks>
///     <para>
///         Use these extensions on Orleans silo hosts. For ASP.NET Core hosts
///         that serve SignalR hubs, use the extensions from <c>Inlet.Orleans.SignalR</c>.
///     </para>
/// </remarks>
public static class InletOrleansServiceCollectionExtensions
{
    /// <summary>
    ///     Adds Inlet Orleans services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers the <see cref="IProjectionBrookRegistry" /> as a singleton.
    ///         You must call <see cref="ScanProjectionAssemblies" /> to populate the registry
    ///         with projection-to-brook mappings from attributed types.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddInletOrleans(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IProjectionBrookRegistry, ProjectionBrookRegistry>();
        return services;
    }

    /// <summary>
    ///     Scans assemblies for projection types and registers them in the brook registry.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for projection types.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method scans the provided assemblies for types decorated with
    ///         <see cref="UxProjectionAttribute" /> and registers their projection-to-brook
    ///         mappings in the <see cref="IProjectionBrookRegistry" />.
    ///     </para>
    ///     <para>
    ///         Call this after <see cref="AddInletOrleans" /> to populate the registry.
    ///     </para>
    /// </remarks>
    public static IServiceCollection ScanProjectionAssemblies(
        this IServiceCollection services,
        params Assembly[] assemblies
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);
        ProjectionBrookRegistry registry = new();
        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in assembly.GetExportedTypes())
            {
                UxProjectionAttribute? attr = type.GetCustomAttribute<UxProjectionAttribute>();
                if (attr is null)
                {
                    continue;
                }

                string brookName = attr.BrookName ?? type.Name;
                string projectionTypeName = type.Name;
                registry.Register(projectionTypeName, brookName);
            }
        }

        services.RemoveAll<IProjectionBrookRegistry>();
        services.AddSingleton<IProjectionBrookRegistry>(registry);
        return services;
    }
}