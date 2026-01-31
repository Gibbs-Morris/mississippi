using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Sdk.Server;

/// <summary>
///     Entry points for Mississippi server registrations.
/// </summary>
public static class MississippiServerRegistrations
{
    /// <summary>
    ///     Adds Mississippi server services and returns a fluent builder wrapper.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="configure">Optional action to configure server options.</param>
    /// <returns>The Mississippi server builder for chaining.</returns>
    public static MississippiServerBuilder AddMississippiServer(
        this WebApplicationBuilder builder,
        Action<MississippiServerOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddOptions<MississippiServerOptions>();
        if (configure != null)
        {
            builder.Services.Configure(configure);
        }

        return new MississippiServerBuilder(builder);
    }
}
