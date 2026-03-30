using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Mississippi.DomainModeling.ReplicaSinks.Abstractions;
using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;
using Mississippi.DomainModeling.Runtime;


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
        services.AddOptions<ReplicaSinkRuntimeOptions>();
        services.TryAddSingleton<IReplicaSinkProjectionRegistry>(serviceProvider => new ReplicaSinkProjectionRegistry(
            serviceProvider,
            serviceProvider.GetServices<ReplicaSinkProjectionDescriptor>(),
            serviceProvider.GetServices<ReplicaSinkRegistrationDescriptor>()));
        services.TryAddSingleton<IReplicaSinkStartupValidator, ReplicaSinkStartupValidator>();
        services.AddUxProjections();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IReplicaSinkSourceStateAccessor, ReplicaSinkUxProjectionSourceStateAccessor>();
        services.TryAddSingleton<IReplicaSinkExecutionHealthManager, ReplicaSinkExecutionHealthManager>();
        services.TryAddSingleton<IReplicaSinkOperatorAuditSink, LoggerReplicaSinkOperatorAuditSink>();
        services.TryAddSingleton<IReplicaSinkLatestStateProcessorHook, NullReplicaSinkLatestStateProcessorHook>();
        services.TryAddSingleton<IReplicaSinkLatestStateProcessor, ReplicaSinkLatestStateProcessor>();
        services.TryAddSingleton<IReplicaSinkRuntimeCoordinator, ReplicaSinkRuntimeCoordinator>();
        services.TryAddSingleton<IReplicaSinkRuntimeOperator, ReplicaSinkRuntimeOperator>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ReplicaSinkStartupValidationService>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ReplicaSinkRuntimeExecutionService>());
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
        List<ReplicaSinkProjectionDescriptor> existingDescriptors = services
            .Where(descriptor => (descriptor.ServiceType == typeof(ReplicaSinkProjectionDescriptor)) &&
                                 descriptor.ImplementationInstance is ReplicaSinkProjectionDescriptor)
            .Select(descriptor => (ReplicaSinkProjectionDescriptor)descriptor.ImplementationInstance!)
            .ToList();
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

        foreach (ReplicaSinkProjectionDescriptor descriptor in descriptors)
        {
            if (existingDescriptors.Contains(descriptor))
            {
                continue;
            }

            services.AddSingleton(descriptor);
        }

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