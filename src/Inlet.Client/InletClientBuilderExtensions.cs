using System;

using Mississippi.Common.Builders.Client.Abstractions;


namespace Mississippi.Inlet.Client;

/// <summary>
///     Client-builder extension methods for Inlet registration.
/// </summary>
public static class InletClientBuilderExtensions
{
    /// <summary>
    ///     Adds Inlet client core services.
    /// </summary>
    /// <param name="builder">Client builder.</param>
    /// <returns>The same client builder instance for fluent chaining.</returns>
    public static IClientBuilder AddInlet(
        this IClientBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddInletClient();
        return builder;
    }

    /// <summary>
    ///     Adds Inlet SignalR integration.
    /// </summary>
    /// <param name="builder">Client builder.</param>
    /// <param name="configure">Optional SignalR builder configuration.</param>
    /// <returns>The same client builder instance for fluent chaining.</returns>
    public static IClientBuilder AddInletSignalR(
        this IClientBuilder builder,
        Action<InletBlazorSignalRBuilder>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddInletBlazorSignalR(configure);
        return builder;
    }
}