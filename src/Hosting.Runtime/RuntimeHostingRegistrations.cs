using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Hosting.Abstractions;

using Orleans.Hosting;


namespace Mississippi.Hosting.Runtime;

/// <summary>
///     Provides terminal Mississippi runtime attachment for Orleans silo hosts.
/// </summary>
/// <remarks>Public so application startup has one validated runtime composition entry point.</remarks>
public static class RuntimeHostingRegistrations
{
    private static ConditionalWeakTable<IServiceCollection, RuntimeAttachment> HostAttachments { get; } = new();

    /// <summary>
    ///     Configures, validates, and attaches Mississippi runtime services once for this host.
    /// </summary>
    /// <param name="siloBuilder">The owning Orleans silo builder.</param>
    /// <param name="configure">The synchronous runtime composition callback.</param>
    /// <returns>The original silo builder.</returns>
    /// <remarks>
    ///     Register services through the runtime builder or its staged native callback. Direct changes to the captured
    ///     host service collection reject attachment and remain on the host; staged runtime registrations are discarded.
    /// </remarks>
    public static ISiloBuilder UseMississippi(
        this ISiloBuilder siloBuilder,
        Action<RuntimeBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(siloBuilder);
        ArgumentNullException.ThrowIfNull(configure);
        ThrowIfHostServicesReadOnly(siloBuilder.Services);
        if (siloBuilder.Services is RuntimeServiceCollection ||
            siloBuilder.Services.Any(descriptor => descriptor.ServiceType == typeof(RuntimeAttachment)) ||
            !HostAttachments.TryAdd(siloBuilder.Services, RuntimeAttachment.Instance))
        {
            throw new BuilderValidationException(
            [
                new(
                    BuilderDiagnosticCodes.DuplicateHostAttachment,
                    "Mississippi runtime services are already attaching or attached to this host.",
                    "Combine runtime composition in one UseMississippi(...) callback."),
            ]);
        }

        ServiceDescriptor attachment = ServiceDescriptor.Singleton(RuntimeAttachment.Instance);
        bool completed = false;
        RuntimeBuilder? runtime = null;
        try
        {
            RuntimeServiceCollection stagedServices = new(attachment);
            runtime = new(siloBuilder, stagedServices);
            siloBuilder.Services.Add(attachment);
            ServiceDescriptor[] originalHostServices = siloBuilder.Services.ToArray();
            foreach (ServiceDescriptor descriptor in originalHostServices.Where(descriptor =>
                         !ReferenceEquals(descriptor, attachment)))
            {
                ((IServiceCollection)stagedServices).Add(descriptor);
            }

            configure(runtime);
            ThrowIfHostServicesChanged(siloBuilder.Services, originalHostServices);
            IReadOnlyList<BuilderDiagnostic> diagnostics = runtime.Validate();
            if (diagnostics.Count > 0)
            {
                throw new BuilderValidationException(diagnostics);
            }

            if (!runtime.IsSiloApplied)
            {
                runtime.ApplyToSilo(siloBuilder);
            }

            ThrowIfHostServicesChanged(siloBuilder.Services, originalHostServices);
            runtime.Complete();
            siloBuilder.Services.Clear();
            foreach (ServiceDescriptor descriptor in stagedServices.Where(descriptor =>
                         descriptor.ServiceType != typeof(RuntimeAttachment)))
            {
                siloBuilder.Services.Add(descriptor);
            }

            siloBuilder.Services.Add(attachment);
            completed = true;
            return siloBuilder;
        }
        finally
        {
            if (!completed)
            {
                try
                {
                    runtime?.Abort();
                    if (!siloBuilder.Services.IsReadOnly)
                    {
                        for (int index = siloBuilder.Services.Count - 1; index >= 0; index--)
                        {
                            if (siloBuilder.Services[index].ServiceType == typeof(RuntimeAttachment))
                            {
                                siloBuilder.Services.RemoveAt(index);
                            }
                        }
                    }
                }
                finally
                {
                    HostAttachments.Remove(siloBuilder.Services);
                }
            }
        }
    }

    private static void ThrowIfHostServicesChanged(
        IServiceCollection services,
        ServiceDescriptor[] originalHostServices
    )
    {
        ThrowIfHostServicesReadOnly(services);
        if (!services.SequenceEqual(originalHostServices))
        {
            throw new BuilderValidationException(
            [
                new(
                    BuilderDiagnosticCodes.HostServicesChanged,
                    "Host services changed during Mississippi runtime composition.",
                    "Register services through runtime.Services or the staged ConfigureSilo(...) callback, or configure the host before UseMississippi(...)."),
            ]);
        }
    }

    private static void ThrowIfHostServicesReadOnly(
        IServiceCollection services
    )
    {
        if (services.IsReadOnly)
        {
            throw new BuilderValidationException(
            [
                new(
                    BuilderDiagnosticCodes.HostServicesReadOnly,
                    "Host services are read-only and cannot accept Mississippi runtime composition.",
                    "Compose Mississippi before the host services become read-only; use a fresh host if they are already frozen."),
            ]);
        }
    }
}