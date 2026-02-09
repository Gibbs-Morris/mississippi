using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.Sdk.Server.Builders;


namespace Mississippi.Sdk.Server;

/// <summary>
///     Extension methods for Mississippi server builder registration.
/// </summary>
public static class MississippiServerBuilderExtensions
{
    /// <summary>
    ///     Creates a Mississippi server builder for a web application.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The Mississippi server builder.</returns>
    public static IMississippiServerBuilder AddMississippiServer(
        this WebApplicationBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new MississippiServerBuilder(builder.Services);
    }

    /// <summary>
    ///     Creates a Mississippi server builder for a host application.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The Mississippi server builder.</returns>
    public static IMississippiServerBuilder AddMississippiServer(
        this HostApplicationBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new MississippiServerBuilder(builder.Services);
    }
}
