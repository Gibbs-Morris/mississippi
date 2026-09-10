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
    /// <exception cref="BuilderValidationException">The host or composition has already been attached.</exception>
    public static WebAssemblyHostBuilder UseMississippi(
        this WebAssemblyHostBuilder builder,
        Action<ClientBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
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
        try
        {
            ServiceCollection stagedServices = [];
            foreach (ServiceDescriptor descriptor in builder.Services.Where(descriptor =>
                         !ReferenceEquals(descriptor, attachment)))
            {
                ((IServiceCollection)stagedServices).Add(descriptor);
            }

            ClientBuilder client = new(stagedServices);
            configure(client);
            IReadOnlyList<BuilderDiagnostic> diagnostics = client.Validate();
            if (diagnostics.Count > 0)
            {
                throw new BuilderValidationException(diagnostics);
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
                builder.Services.Remove(attachment);
            }
        }
    }
}