using System;

using Microsoft.AspNetCore.SignalR;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Common.Builders.Gateway.Abstractions;


namespace Mississippi.Aqueduct.Gateway;

/// <summary>
///     Gateway-builder extension methods for Aqueduct registration.
/// </summary>
public static class AqueductGatewayBuilderExtensions
{
    /// <summary>
    ///     Adds Aqueduct backplane services for the specified hub type.
    /// </summary>
    /// <typeparam name="THub">Hub type.</typeparam>
    /// <param name="builder">Gateway builder.</param>
    /// <param name="configureOptions">Optional options configuration.</param>
    /// <returns>The same gateway builder instance for fluent chaining.</returns>
    public static IGatewayBuilder AddAqueduct<THub>(
        this IGatewayBuilder builder,
        Action<AqueductOptions>? configureOptions = null
    )
        where THub : Hub
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (configureOptions is null)
        {
            builder.Services.AddAqueduct<THub>();
        }
        else
        {
            builder.Services.AddAqueduct<THub>(configureOptions);
        }

        return builder;
    }
}