using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Inlet.Runtime.Abstractions;


namespace Mississippi.Inlet.Runtime;

/// <summary>
///     Internal registration helpers for Inlet runtime services.
/// </summary>
/// <remarks>
///     <para>
///         Use these extensions on Orleans silo hosts. For ASP.NET Core hosts
///         that serve SignalR hubs, use the extensions from <c>Inlet.Gateway</c>.
///     </para>
/// </remarks>
internal static class InletSiloRegistrations
{
    /// <summary>
    ///     Adds Inlet Orleans services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method registers the <see cref="IProjectionBrookRegistry" /> as a singleton.
    ///         It also registers <see cref="IProjectionAuthorizationRegistry" /> as a singleton.
    ///     </para>
    /// </remarks>
    public static IServiceCollection AddInletSilo(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IProjectionBrookRegistry, ProjectionBrookRegistry>();
        services.TryAddSingleton<IProjectionAuthorizationRegistry, ProjectionAuthorizationRegistry>();
        return services;
    }
}