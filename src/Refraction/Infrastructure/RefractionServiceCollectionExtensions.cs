using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Refraction.Infrastructure;

/// <summary>
///     Extension methods for registering Refraction services.
/// </summary>
public static class RefractionServiceCollectionExtensions
{
    /// <summary>
    ///     Adds Refraction component library services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         Refraction components are pure presentation and do not require services for basic usage.
    ///         This method registers optional services for advanced scenarios:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Theme management</description>
    ///         </item>
    ///         <item>
    ///             <description>Focus tracking</description>
    ///         </item>
    ///         <item>
    ///             <description>Motion preferences</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static IServiceCollection AddRefraction(
        this IServiceCollection services
    ) =>
        services;
}