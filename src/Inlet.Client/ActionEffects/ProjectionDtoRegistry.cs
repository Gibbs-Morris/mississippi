using System;
using System.Collections.Concurrent;
using System.Reflection;

using Mississippi.Inlet.Projection.Abstractions;


namespace Mississippi.Inlet.Client.ActionEffects;

/// <summary>
///     Thread-safe implementation of <see cref="IProjectionDtoRegistry" />.
/// </summary>
/// <remarks>
///     <para>
///         This registry maintains bidirectional mappings between projection paths
///         and DTO types. It is populated at application startup by calling
///         <see cref="ScanAssemblies" /> with assemblies containing
///         <see cref="ProjectionPathAttribute" />-decorated types.
///     </para>
/// </remarks>
internal sealed class ProjectionDtoRegistry : IProjectionDtoRegistry
{
    private ConcurrentDictionary<Type, string> DtoTypeToPath { get; } = new();

    private ConcurrentDictionary<string, Type> PathToDtoType { get; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Type? GetDtoType(
        string path
    )
    {
        ArgumentNullException.ThrowIfNull(path);
        return PathToDtoType.TryGetValue(path, out Type? dtoType) ? dtoType : null;
    }

    /// <inheritdoc />
    public string? GetPath(
        Type dtoType
    )
    {
        ArgumentNullException.ThrowIfNull(dtoType);
        return DtoTypeToPath.TryGetValue(dtoType, out string? path) ? path : null;
    }

    /// <inheritdoc />
    public void Register(
        string path,
        Type dtoType
    )
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(dtoType);
        PathToDtoType[path] = dtoType;
        DtoTypeToPath[dtoType] = path;
    }

    /// <inheritdoc />
    public void ScanAssemblies(
        params Assembly[] assemblies
    )
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        foreach (Assembly assembly in assemblies)
        {
            ScanAssembly(assembly);
        }
    }

    /// <inheritdoc />
    public bool TryGetDtoType(
        string path,
        out Type? dtoType
    )
    {
        ArgumentNullException.ThrowIfNull(path);
        return PathToDtoType.TryGetValue(path, out dtoType);
    }

    /// <inheritdoc />
    public bool TryGetPath(
        Type dtoType,
        out string? path
    )
    {
        ArgumentNullException.ThrowIfNull(dtoType);
        return DtoTypeToPath.TryGetValue(dtoType, out path);
    }

    private void ScanAssembly(
        Assembly assembly
    )
    {
        foreach (Type type in assembly.GetTypes())
        {
            ProjectionPathAttribute? attr = type.GetCustomAttribute<ProjectionPathAttribute>();
            if (attr is not null)
            {
                Register(attr.Path, type);
            }
        }
    }
}