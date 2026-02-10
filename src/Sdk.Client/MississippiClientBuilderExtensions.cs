using System;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Hosting;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.Sdk.Client.Builders;


namespace Mississippi.Sdk.Client;

/// <summary>
///     Extension methods for Mississippi client builder registration.
/// </summary>
public static class MississippiClientBuilderExtensions
{
    /// <summary>
    ///     Creates a Mississippi client builder for a host application.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The Mississippi client builder.</returns>
    public static IMississippiClientBuilder AddMississippiClient(
        this HostApplicationBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new MississippiClientBuilder(builder.Services);
    }

    /// <summary>
    ///     Creates a Mississippi client builder for a WebAssembly host.
    /// </summary>
    /// <param name="builder">The WebAssembly host builder.</param>
    /// <returns>The Mississippi client builder.</returns>
    public static IMississippiClientBuilder AddMississippiClient(
        this WebAssemblyHostBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new MississippiClientBuilder(builder.Services);
    }
}