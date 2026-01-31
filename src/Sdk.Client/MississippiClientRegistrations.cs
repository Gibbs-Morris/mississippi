using System;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Sdk.Client;

/// <summary>
///     Entry points for Mississippi client registrations.
/// </summary>
public static class MississippiClientRegistrations
{
    /// <summary>
    ///     Adds Mississippi client services and returns a fluent builder wrapper.
    /// </summary>
    /// <param name="builder">The WebAssembly host builder.</param>
    /// <param name="configure">Optional action to configure client options.</param>
    /// <returns>The Mississippi client builder for chaining.</returns>
    public static MississippiClientBuilder AddMississippiClient(
        this WebAssemblyHostBuilder builder,
        Action<MississippiClientOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddOptions<MississippiClientOptions>();
        if (configure != null)
        {
            builder.Services.Configure(configure);
        }

        return new(builder);
    }
}