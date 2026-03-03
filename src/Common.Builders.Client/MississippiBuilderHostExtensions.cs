using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Abstractions;
using Mississippi.Common.Builders.Client.Abstractions;


namespace Mississippi.Common.Builders.Client;

/// <summary>
///     Host adapter extensions for attaching Mississippi client builders in WebAssembly hosts.
/// </summary>
public static class MississippiBuilderHostExtensions
{
    /// <summary>
    ///     Attaches a client builder to a WebAssembly host and merges configured services.
    /// </summary>
    /// <param name="webBuilder">WebAssembly host builder.</param>
    /// <param name="builder">Client builder.</param>
    /// <returns>The same WebAssembly host builder instance.</returns>
    public static WebAssemblyHostBuilder UseMississippi(
        this WebAssemblyHostBuilder webBuilder,
        IClientBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(webBuilder);
        ArgumentNullException.ThrowIfNull(builder);
        EnsureValid(builder);
        EnsureNotAlreadyAttached(webBuilder.Services);
        MergeServices(webBuilder.Services, builder.Services);
        MarkAttached(webBuilder.Services);
        return webBuilder;
    }

    private static void EnsureNotAlreadyAttached(
        IServiceCollection services
    )
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(MississippiBuilderAttachmentMarker)))
        {
            throw new InvalidOperationException("UseMississippi() can only be called once per host builder.");
        }
    }

    private static void EnsureValid(
        IMississippiBuilder builder
    )
    {
        IReadOnlyList<BuilderDiagnostic> diagnostics = builder.Validate();
        if (diagnostics.Count == 0)
        {
            return;
        }

        throw new BuilderValidationException("Builder validation failed before terminal host attachment.", diagnostics);
    }

    private static void MarkAttached(
        IServiceCollection services
    )
    {
        services.AddSingleton<MississippiBuilderAttachmentMarker>();
    }

    private static void MergeServices(
        IServiceCollection target,
        IServiceCollection source
    )
    {
        foreach (ServiceDescriptor descriptor in source)
        {
            target.Add(descriptor);
        }
    }

    private sealed class MississippiBuilderAttachmentMarker
    {
        public override string ToString() => nameof(MississippiBuilderAttachmentMarker);
    }
}