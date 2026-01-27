using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.ActionEffects;


namespace Mississippi.Inlet.Client;

/// <summary>
///     Extension methods for adding Inlet Blazor services.
/// </summary>
public static class InletBlazorRegistrations
{
    /// <summary>
    ///     Adds Inlet Blazor services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is null.</exception>
    /// <remarks>
    ///     This must be called after <c>AddInlet()</c>.
    /// </remarks>
    public static IServiceCollection AddInletBlazor(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }

    /// <summary>
    ///     Adds Inlet Blazor SignalR services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for the builder.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is null.</exception>
    /// <remarks>
    ///     <para>
    ///         This registers the <see cref="InletSignalRActionEffect" /> which handles projection
    ///         subscription actions via SignalR. The effect requires an <see cref="IProjectionFetcher" />
    ///         to be registered.
    ///     </para>
    ///     <para>
    ///         Use the builder to configure the hub path and register projection fetchers.
    ///     </para>
    ///     <example>
    ///         <code>
    /// services.AddInletBlazorSignalR(builder =>
    /// {
    ///     builder.WithHubPath("/hubs/inlet")
    ///            .AddProjectionFetcher&lt;MyProjectionFetcher&gt;();
    /// });
    ///         </code>
    ///     </example>
    /// </remarks>
    public static IServiceCollection AddInletBlazorSignalR(
        this IServiceCollection services,
        Action<InletBlazorSignalRBuilder>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        InletBlazorSignalRBuilder builder = new(services);
        configure?.Invoke(builder);
        builder.Build();
        return services;
    }
}