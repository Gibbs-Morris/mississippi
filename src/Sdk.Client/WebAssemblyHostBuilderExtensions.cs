using System;
using System.Linq;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Diagnostics;


namespace Mississippi.Sdk.Client;

/// <summary>
///     Extension methods for attaching Mississippi client composition to a Blazor WebAssembly host.
/// </summary>
public static class WebAssemblyHostBuilderExtensions
{
    /// <summary>
    ///     Attaches Mississippi client composition to the Blazor WebAssembly host builder.
    /// </summary>
    /// <param name="builder">The WebAssembly host builder.</param>
    /// <param name="configure">The client configuration callback.</param>
    /// <returns>The same host builder for chaining.</returns>
    /// <exception cref="MississippiBuilderException">
    ///     Thrown with <see cref="MississippiDiagnosticCodes.ClientDuplicateAttach" />
    ///     if <c>UseMississippi</c> has already been called on this builder.
    /// </exception>
    public static WebAssemblyHostBuilder UseMississippi(
        this WebAssemblyHostBuilder builder,
        Action<MississippiClientBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        if (builder.Services.Any(d => d.ServiceType == typeof(MississippiClientAttachMarker)))
        {
            throw new MississippiBuilderException(
                MississippiDiagnosticCodes.ClientDuplicateAttach,
                "UseMississippi was called more than once on the same WebAssemblyHostBuilder.");
        }

        MississippiClientBuilder clientBuilder = new(builder.Services);
        configure(clientBuilder);
        clientBuilder.Validate();
        builder.Services.AddSingleton(MississippiClientAttachMarker.Instance);
        return builder;
    }

    private sealed class MississippiClientAttachMarker
    {
        public static MississippiClientAttachMarker Instance { get; } = new();
    }
}