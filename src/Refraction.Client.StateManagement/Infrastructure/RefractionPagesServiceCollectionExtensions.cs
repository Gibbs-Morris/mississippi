using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Refraction.Client.StateManagement.Infrastructure;

/// <summary>
///     Extension methods for registering Refraction.Client.StateManagement services.
/// </summary>
public static class RefractionPagesServiceCollectionExtensions
{
    /// <summary>
    ///     Adds Refraction.Client.StateManagement services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     This method registers scene-level services. Call this in addition to
    ///     <c>UseRefraction()</c> and <c>AddReservoir()</c> in your application startup.
    /// </remarks>
    public static IServiceCollection UseRefractionPages(
        this IServiceCollection services
    ) =>
        services;
}