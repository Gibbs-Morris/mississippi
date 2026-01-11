using System;
using System.Reflection;


namespace Mississippi.Inlet.Blazor.WebAssembly.Effects;

/// <summary>
///     Registry that maps projection route strings to client-side DTO types.
/// </summary>
/// <remarks>
///     <para>
///         This registry is populated by scanning assemblies for types decorated with
///         <see cref="UxProjectionDtoAttribute" />. The <see cref="AutoProjectionFetcher" />
///         uses this registry to determine which DTO type to deserialize based on
///         the route string received in SignalR notifications or derived from the
///         projection type name.
///     </para>
/// </remarks>
public interface IProjectionDtoRegistry
{
    /// <summary>
    ///     Gets the DTO type for the specified route.
    /// </summary>
    /// <param name="route">The route string (e.g., "channel-messages").</param>
    /// <returns>The DTO type if registered; otherwise, <c>null</c>.</returns>
    Type? GetDtoType(string route);

    /// <summary>
    ///     Gets the route for the specified DTO type.
    /// </summary>
    /// <param name="dtoType">The DTO type.</param>
    /// <returns>The route string if registered; otherwise, <c>null</c>.</returns>
    string? GetRoute(Type dtoType);

    /// <summary>
    ///     Registers a DTO type for the specified route.
    /// </summary>
    /// <param name="route">The route string.</param>
    /// <param name="dtoType">The DTO type.</param>
    void Register(string route, Type dtoType);

    /// <summary>
    ///     Scans the specified assemblies for types decorated with
    ///     <see cref="UxProjectionDtoAttribute" /> and registers them.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    void ScanAssemblies(params Assembly[] assemblies);

    /// <summary>
    ///     Tries to get the DTO type for the specified route.
    /// </summary>
    /// <param name="route">The route string.</param>
    /// <param name="dtoType">When this method returns, contains the DTO type if found.</param>
    /// <returns><c>true</c> if the route was found; otherwise, <c>false</c>.</returns>
    bool TryGetDtoType(string route, out Type? dtoType);

    /// <summary>
    ///     Tries to get the route for the specified DTO type.
    /// </summary>
    /// <param name="dtoType">The DTO type.</param>
    /// <param name="route">When this method returns, contains the route if found.</param>
    /// <returns><c>true</c> if the DTO type was found; otherwise, <c>false</c>.</returns>
    bool TryGetRoute(Type dtoType, out string? route);
}
