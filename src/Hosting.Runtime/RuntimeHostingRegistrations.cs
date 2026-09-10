using System;
using System.Collections.Generic;
using System.Linq;

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
    /// <summary>
    ///     Configures, validates, and attaches Mississippi runtime services once for this host.
    /// </summary>
    /// <param name="siloBuilder">The owning Orleans silo builder.</param>
    /// <param name="configure">The synchronous runtime composition callback.</param>
    /// <returns>The original silo builder.</returns>
    public static ISiloBuilder UseMississippi(
        this ISiloBuilder siloBuilder,
        Action<RuntimeBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(siloBuilder);
        ArgumentNullException.ThrowIfNull(configure);
        if (siloBuilder.Services.Any(descriptor => descriptor.ServiceType == typeof(RuntimeAttachment)))
        {
            throw new BuilderValidationException(
            [
                new(
                    BuilderDiagnosticCodes.DuplicateHostAttachment,
                    "Mississippi runtime services are already attaching or attached to this host.",
                    "Combine runtime composition in one UseMississippi(...) callback."),
            ]);
        }

        ServiceCollection stagedServices = [];
        RuntimeBuilder runtime = new(siloBuilder, stagedServices);
        ServiceDescriptor attachment = ServiceDescriptor.Singleton(RuntimeAttachment.Instance);
        siloBuilder.Services.Add(attachment);
        bool completed = false;
        try
        {
            foreach (ServiceDescriptor descriptor in siloBuilder.Services.Where(descriptor =>
                         !ReferenceEquals(descriptor, attachment)))
            {
                ((IServiceCollection)stagedServices).Add(descriptor);
            }

            configure(runtime);
            IReadOnlyList<BuilderDiagnostic> diagnostics = runtime.Validate();
            if (diagnostics.Count > 0)
            {
                throw new BuilderValidationException(diagnostics);
            }

            if (!runtime.IsSiloApplied)
            {
                runtime.ApplyToSilo(siloBuilder);
            }

            runtime.Complete();
            siloBuilder.Services.Clear();
            foreach (ServiceDescriptor descriptor in stagedServices)
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
                runtime.Abort();
                siloBuilder.Services.Remove(attachment);
            }
        }
    }
}