using System;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Diagnostics;


namespace Mississippi.Sdk.Gateway;

/// <summary>
///     Extension methods for attaching Mississippi gateway composition to a web application.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    ///     Attaches Mississippi gateway composition to the web application builder.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="configure">The gateway configuration callback.</param>
    /// <returns>The same web application builder for chaining.</returns>
    /// <exception cref="MississippiBuilderException">
    ///     Thrown with <see cref="MississippiDiagnosticCodes.DuplicateAttach" />
    ///     if <c>UseMississippi</c> has already been called on this builder.
    /// </exception>
    public static WebApplicationBuilder UseMississippi(
        this WebApplicationBuilder builder,
        Action<MississippiGatewayBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        if (builder.Services.Any(d => d.ServiceType == typeof(MississippiGatewayAttachMarker)))
        {
            throw new MississippiBuilderException(
                MississippiDiagnosticCodes.DuplicateAttach,
                "UseMississippi was called more than once on the same WebApplicationBuilder.");
        }

        MississippiGatewayBuilder gatewayBuilder = new(builder.Services);
        configure(gatewayBuilder);
        gatewayBuilder.Validate();
        builder.Services.AddSingleton(MississippiGatewayAttachMarker.Instance);
        return builder;
    }

    private sealed class MississippiGatewayAttachMarker
    {
        public static MississippiGatewayAttachMarker Instance { get; } = new();
    }
}