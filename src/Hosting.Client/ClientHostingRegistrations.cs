using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Hosting.Abstractions;


namespace Mississippi.Hosting.Client;

/// <summary>
///     Attaches Mississippi client composition to a WebAssembly host.
/// </summary>
/// <remarks>Public to provide the canonical terminal client attachment API.</remarks>
public static class ClientHostingRegistrations
{
    /// <summary>
    ///     Configures, validates, and attaches Mississippi client services once.
    /// </summary>
    /// <param name="builder">The host receiving the client services.</param>
    /// <param name="configure">The client composition callback.</param>
    /// <returns>The original host builder.</returns>
    /// <remarks>
    ///     Register services through the supplied client inside the callback. Direct changes to the captured host service
    ///     collection reject attachment and remain on the host; staged client registrations are discarded.
    /// </remarks>
    /// <exception cref="BuilderValidationException">
    ///     The host or composition has already been attached, host services are read-only, or they changed during
    ///     configuration.
    /// </exception>
    public static WebAssemblyHostBuilder UseMississippi(
        this WebAssemblyHostBuilder builder,
        Action<ClientBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        ThrowIfHostServicesReadOnly(builder.Services);
        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(ClientAttachment)))
        {
            throw new BuilderValidationException(
            [
                new(
                    BuilderDiagnosticCodes.DuplicateHostAttachment,
                    "Mississippi client services are already attached to this host.",
                    "Combine client configuration in one UseMississippi(...) call."),
            ]);
        }

        ServiceDescriptor attachment = ServiceDescriptor.Singleton(ClientAttachment.Instance);
        builder.Services.Add(attachment);
        bool completed = false;
        ClientBuilder? client = null;
        try
        {
            ServiceDescriptor[] originalHostServices = builder.Services.ToArray();
            ServiceCollection stagedServices = [];
            foreach (ServiceDescriptor descriptor in originalHostServices.Where(descriptor =>
                         !ReferenceEquals(descriptor, attachment)))
            {
                ((IServiceCollection)stagedServices).Add(descriptor);
            }

            client = new(stagedServices);
            configure(client);
            ThrowIfHostServicesReadOnly(builder.Services);
            IReadOnlyList<BuilderDiagnostic> diagnostics = client.Validate();
            if (diagnostics.Count > 0)
            {
                throw new BuilderValidationException(diagnostics);
            }

            if (!builder.Services.SequenceEqual(originalHostServices))
            {
                throw new BuilderValidationException(
                [
                    new(
                        BuilderDiagnosticCodes.HostServicesChanged,
                        "Host services changed during Mississippi client composition.",
                        "Register services through client.Services inside UseMississippi(...), or configure the host before calling it."),
                ]);
            }

            client.Complete();
            builder.Services.Clear();
            foreach (ServiceDescriptor descriptor in stagedServices)
            {
                builder.Services.Add(descriptor);
            }

            builder.Services.Add(attachment);
            completed = true;
            return builder;
        }
        finally
        {
            if (!completed)
            {
                client?.Abort();
                if (!builder.Services.IsReadOnly)
                {
                    for (int index = builder.Services.Count - 1; index >= 0; index--)
                    {
                        if (builder.Services[index].ServiceType == typeof(ClientAttachment))
                        {
                            builder.Services.RemoveAt(index);
                        }
                    }
                }
            }
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
                    "Host services are read-only and cannot accept Mississippi client composition.",
                    "Compose Mississippi before the host services become read-only; use a fresh host if they are already frozen."),
            ]);
        }
    }
}