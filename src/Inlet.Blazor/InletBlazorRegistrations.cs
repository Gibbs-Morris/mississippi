using System;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Inlet.Blazor;

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
}