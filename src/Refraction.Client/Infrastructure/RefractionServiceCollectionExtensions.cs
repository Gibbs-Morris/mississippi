using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Refraction.Abstractions.Theme;


namespace Mississippi.Refraction.Client.Infrastructure;

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
    ///         Refraction components are pure presentation and do not require store coupling for basic usage.
    ///         This method registers the default Slice 1 runtime foundation for theme selection:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>default theme catalog resolution</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static IServiceCollection AddRefraction(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IRefractionThemeCatalog, DefaultRefractionThemeCatalog>();
        return services;
    }
}