using System;
using System.Collections.Concurrent;
using System.Reflection;


namespace Mississippi.Inlet.Blazor.WebAssembly.Effects;

/// <summary>
///     Thread-safe implementation of <see cref="IProjectionDtoRegistry" />.
/// </summary>
/// <remarks>
///     <para>
///         This registry maintains bidirectional mappings between route strings
///         and DTO types. It is populated at application startup by calling
///         <see cref="ScanAssemblies" /> with assemblies containing
///         <see cref="UxProjectionDtoAttribute" />-decorated types.
///     </para>
/// </remarks>
internal sealed class ProjectionDtoRegistry : IProjectionDtoRegistry
{
    private ConcurrentDictionary<Type, string> DtoTypeToRoute { get; } = new();

    private ConcurrentDictionary<string, Type> RouteToDtoType { get; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Type? GetDtoType(string route)
    {
        ArgumentNullException.ThrowIfNull(route);
        return RouteToDtoType.TryGetValue(route, out Type? dtoType) ? dtoType : null;
    }

    /// <inheritdoc />
    public string? GetRoute(Type dtoType)
    {
        ArgumentNullException.ThrowIfNull(dtoType);
        return DtoTypeToRoute.TryGetValue(dtoType, out string? route) ? route : null;
    }

    /// <inheritdoc />
    public void Register(string route, Type dtoType)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(dtoType);
        RouteToDtoType[route] = dtoType;
        DtoTypeToRoute[dtoType] = route;
    }

    /// <inheritdoc />
    public void ScanAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (Assembly assembly in assemblies)
        {
            ScanAssembly(assembly);
        }
    }

    /// <inheritdoc />
    public bool TryGetDtoType(string route, out Type? dtoType)
    {
        ArgumentNullException.ThrowIfNull(route);
        return RouteToDtoType.TryGetValue(route, out dtoType);
    }

    /// <inheritdoc />
    public bool TryGetRoute(Type dtoType, out string? route)
    {
        ArgumentNullException.ThrowIfNull(dtoType);
        return DtoTypeToRoute.TryGetValue(dtoType, out route);
    }

    private void ScanAssembly(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
        {
            UxProjectionDtoAttribute? attr = type.GetCustomAttribute<UxProjectionDtoAttribute>();
            if (attr is not null)
            {
                Register(attr.Route, type);
            }
        }
    }
}
