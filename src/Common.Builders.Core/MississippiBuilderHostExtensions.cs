using System;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.Common.Builders.Client.Abstractions;
using Mississippi.Common.Builders.Gateway;
using Mississippi.Common.Builders.Gateway.Abstractions;
using Mississippi.Common.Builders.Runtime.Abstractions;


namespace Mississippi.Common.Builders.Core;

/// <summary>
///     Host adapter extensions for attaching Mississippi builders.
/// </summary>
public static class MississippiBuilderHostExtensions
{
    /// <summary>
    ///     Attaches a client builder to the host and merges configured services.
    /// </summary>
    /// <param name="hostBuilder">Host builder.</param>
    /// <param name="builder">Client builder.</param>
    /// <returns>The same host builder instance.</returns>
    public static HostApplicationBuilder UseMississippi(
        this HostApplicationBuilder hostBuilder,
        IClientBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(builder);
        EnsureNotAlreadyAttached(hostBuilder.Services);
        MergeServices(hostBuilder.Services, builder.Services);
        MarkAttached(hostBuilder.Services);
        return hostBuilder;
    }

    /// <summary>
    ///     Attaches a gateway builder to the host and merges configured services.
    /// </summary>
    /// <param name="hostBuilder">Host builder.</param>
    /// <param name="builder">Gateway builder.</param>
    /// <returns>The same host builder instance.</returns>
    public static HostApplicationBuilder UseMississippi(
        this HostApplicationBuilder hostBuilder,
        IGatewayBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(builder);
        if (builder is GatewayBuilder gatewayBuilder)
        {
            gatewayBuilder.EnsureAuthorizationConfigured();
        }

        EnsureNotAlreadyAttached(hostBuilder.Services);
        MergeServices(hostBuilder.Services, builder.Services);
        MarkAttached(hostBuilder.Services);
        return hostBuilder;
    }

    /// <summary>
    ///     Attaches a runtime builder to the host and merges configured services.
    /// </summary>
    /// <param name="hostBuilder">Host builder.</param>
    /// <param name="builder">Runtime builder.</param>
    /// <returns>The same host builder instance.</returns>
    public static HostApplicationBuilder UseMississippi(
        this HostApplicationBuilder hostBuilder,
        IRuntimeBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(builder);
        EnsureNotAlreadyAttached(hostBuilder.Services);
        MergeServices(hostBuilder.Services, builder.Services);
        MarkAttached(hostBuilder.Services);
        return hostBuilder;
    }

    /// <summary>
    ///     Attaches a client builder to a web host and merges configured services.
    /// </summary>
    /// <param name="webBuilder">Web application builder.</param>
    /// <param name="builder">Client builder.</param>
    /// <returns>The same web builder instance.</returns>
    public static WebApplicationBuilder UseMississippi(
        this WebApplicationBuilder webBuilder,
        IClientBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(webBuilder);
        ArgumentNullException.ThrowIfNull(builder);
        EnsureNotAlreadyAttached(webBuilder.Services);
        MergeServices(webBuilder.Services, builder.Services);
        MarkAttached(webBuilder.Services);
        return webBuilder;
    }

    /// <summary>
    ///     Attaches a gateway builder to a web host and merges configured services.
    /// </summary>
    /// <param name="webBuilder">Web application builder.</param>
    /// <param name="builder">Gateway builder.</param>
    /// <returns>The same web builder instance.</returns>
    public static WebApplicationBuilder UseMississippi(
        this WebApplicationBuilder webBuilder,
        IGatewayBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(webBuilder);
        ArgumentNullException.ThrowIfNull(builder);
        if (builder is GatewayBuilder gatewayBuilder)
        {
            gatewayBuilder.EnsureAuthorizationConfigured();
        }

        EnsureNotAlreadyAttached(webBuilder.Services);
        MergeServices(webBuilder.Services, builder.Services);
        MarkAttached(webBuilder.Services);
        return webBuilder;
    }

    /// <summary>
    ///     Attaches a runtime builder to a web host and merges configured services.
    /// </summary>
    /// <param name="webBuilder">Web application builder.</param>
    /// <param name="builder">Runtime builder.</param>
    /// <returns>The same web builder instance.</returns>
    public static WebApplicationBuilder UseMississippi(
        this WebApplicationBuilder webBuilder,
        IRuntimeBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(webBuilder);
        ArgumentNullException.ThrowIfNull(builder);
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

    private sealed class MississippiBuilderAttachmentMarker;
}