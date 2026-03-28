using System;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Hosting.Gateway;

/// <summary>
///     Extension methods for composing Mississippi gateway applications from a web application builder.
/// </summary>
public static class MississippiGatewayWebApplicationBuilderRegistrations
{
    private const string RuntimeHostModeMarkerFullName = "Mississippi.Hosting.Runtime.MississippiRuntimeHostModeMarker";

    /// <summary>
    ///     Adds the top-level Mississippi gateway builder to a web application builder.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The Mississippi gateway builder for further gateway composition.</returns>
    public static MississippiGatewayBuilder AddMississippiGateway(
        this WebApplicationBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (builder.Services.Any(descriptor => descriptor.ServiceType.FullName == RuntimeHostModeMarkerFullName))
        {
            throw new InvalidOperationException(
                "Mississippi runtime and gateway composition cannot share the same host in this rollout. Remove the runtime host composition from this process and keep the gateway host on the supported gateway-only path.");
        }

        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(MississippiGatewayHostModeMarker)))
        {
            throw new InvalidOperationException(
                "AddMississippiGateway can only be called once per host. Compose additional gateway configuration through the existing MississippiGatewayBuilder instance.");
        }

        builder.Services.AddSingleton(MississippiGatewayHostModeMarker.Instance);
        return new(builder);
    }

    /// <summary>
    ///     Adds the top-level Mississippi gateway builder to a web application builder and invokes a configuration callback.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="configure">The gateway composition callback.</param>
    /// <returns>The original web application builder.</returns>
    public static WebApplicationBuilder AddMississippiGateway(
        this WebApplicationBuilder builder,
        Action<MississippiGatewayBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        configure(builder.AddMississippiGateway());
        return builder;
    }
}