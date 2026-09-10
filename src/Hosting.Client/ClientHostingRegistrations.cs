using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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
    private static ConditionalWeakTable<IServiceCollection, ClientAttachment> HostAttachments { get; } = new();

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
        if (HostAttachments.TryGetValue(builder.Services, out ClientAttachment? previous) && previous.IsDamaged)
        {
            throw new BuilderValidationException(
            [
                new(
                    BuilderDiagnosticCodes.HostServicesDamaged,
                    "The client host service graph could not be restored after failed publication.",
                    "Create a fresh host and configure it again; this service collection cannot be reused."),
            ]);
        }

        ThrowIfHostServicesReadOnly(builder.Services);
        ClientAttachment state = new();
        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(ClientAttachment)) ||
            !HostAttachments.TryAdd(builder.Services, state))
        {
            throw new BuilderValidationException(
            [
                new(
                    BuilderDiagnosticCodes.DuplicateHostAttachment,
                    "Mississippi client services are already attaching or attached to this host.",
                    "Combine client configuration in one UseMississippi(...) call."),
            ]);
        }

        ServiceDescriptor attachment = ServiceDescriptor.Singleton(state);
        bool completed = false;
        bool canReuseHost = true;
        ClientBuilder? client = null;
        try
        {
            builder.Services.Add(attachment);
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
            PublishServices(builder.Services, stagedServices, originalHostServices, attachment, ref canReuseHost);
            completed = true;
            return builder;
        }
        finally
        {
            if (!completed)
            {
                client?.Abort();
                state.IsDamaged = true;
                if (canReuseHost)
                {
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

                    HostAttachments.Remove(builder.Services);
                }
            }
        }
    }

    private static void PublishServices(
        IServiceCollection hostServices,
        IEnumerable<ServiceDescriptor> stagedServices,
        IReadOnlyList<ServiceDescriptor> originalHostServices,
        ServiceDescriptor attachment,
        ref bool canReuseHost
    )
    {
        canReuseHost = false;
        try
        {
            hostServices.Clear();
            foreach (ServiceDescriptor descriptor in stagedServices)
            {
                hostServices.Add(descriptor);
            }

            hostServices.Add(attachment);
        }
        catch (Exception publicationException) when (publicationException is not (OutOfMemoryException
                                                         or AccessViolationException or StackOverflowException))
        {
            try
            {
                hostServices.Clear();
                foreach (ServiceDescriptor descriptor in originalHostServices.Where(descriptor =>
                             !ReferenceEquals(descriptor, attachment)))
                {
                    hostServices.Add(descriptor);
                }

                canReuseHost = true;
            }
            catch (Exception restorationException) when (restorationException is not (OutOfMemoryException
                                                             or AccessViolationException or StackOverflowException))
            {
                throw new AggregateException(
                    "Client service publication failed and the original host registrations could not be restored. Use a fresh host.",
                    publicationException,
                    restorationException);
            }

            throw;
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