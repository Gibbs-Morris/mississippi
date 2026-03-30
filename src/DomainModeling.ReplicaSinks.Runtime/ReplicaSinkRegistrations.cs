using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Mississippi.DomainModeling.ReplicaSinks.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Provides the thin composition shell for replica sink onboarding.
/// </summary>
public static class ReplicaSinkRegistrations
{
    /// <summary>
    ///     Adds replica sink discovery and startup validation services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddReplicaSinks(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IReplicaSinkProjectionRegistry>(_ => new ReplicaSinkProjectionRegistry());
        services.TryAddSingleton<IReplicaSinkStartupValidator, ReplicaSinkStartupValidator>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ReplicaSinkStartupValidationService>());
        return services;
    }

    /// <summary>
    ///     Scans the provided assemblies for projection types decorated with <see cref="ProjectionReplicationAttribute" />.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection ScanReplicaSinkAssemblies(
        this IServiceCollection services,
        params Assembly[] assemblies
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);
        services.AddReplicaSinks();
        List<ReplicaSinkProjectionDescriptor> descriptors = [];
        foreach (Assembly assembly in assemblies)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            foreach (Type type in assembly.GetTypes())
            {
                foreach (ProjectionReplicationAttribute attribute in type
                             .GetCustomAttributes<ProjectionReplicationAttribute>())
                {
                    descriptors.Add(
                        new(
                            type,
                            attribute.Sink,
                            attribute.TargetName,
                            attribute.ContractType,
                            attribute.IsDirectProjectionReplicationEnabled,
                            attribute.WriteMode));
                }
            }
        }

        services.RemoveAll<IReplicaSinkProjectionRegistry>();
        services.AddSingleton<IReplicaSinkProjectionRegistry>(new ReplicaSinkProjectionRegistry(descriptors));
        return services;
    }

    /// <summary>
    ///     Scans the assembly containing <typeparamref name="TMarker" /> for replica sink bindings.
    /// </summary>
    /// <typeparam name="TMarker">A type in the assembly to scan.</typeparam>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection ScanReplicaSinkAssemblies<TMarker>(
        this IServiceCollection services
    ) =>
        services.ScanReplicaSinkAssemblies(typeof(TMarker).Assembly);
}