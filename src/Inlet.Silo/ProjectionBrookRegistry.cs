using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Mississippi.Inlet.Silo.Abstractions;


namespace Mississippi.Inlet.Silo;

/// <summary>
///     Thread-safe implementation of <see cref="IProjectionBrookRegistry" />.
/// </summary>
/// <remarks>
///     <para>
///         This registry maintains a mapping from projection paths to their
///         associated brook names. It is populated at application startup by
///         generated registration code.
///     </para>
///     <para>
///         The registry is designed to be registered as a singleton in the DI container
///         and accessed by <see cref="Grains.InletSubscriptionGrain" /> to resolve brook names
///         when clients subscribe to projections.
///     </para>
/// </remarks>
internal sealed class ProjectionBrookRegistry : IProjectionBrookRegistry
{
    private ConcurrentDictionary<string, bool> AutoDerivedPaths { get; } = new(StringComparer.Ordinal);

    private ConcurrentDictionary<string, string> PathToBrook { get; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IEnumerable<string> GetAllPaths() => PathToBrook.Keys;

    /// <inheritdoc />
    public string? GetBrookName(
        string path
    )
    {
        ArgumentNullException.ThrowIfNull(path);
        return PathToBrook.TryGetValue(path, out string? brookName) ? brookName : null;
    }

    /// <inheritdoc />
    public void Register(
        string path,
        string brookName,
        bool isExplicit = true
    )
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(brookName);
        PathToBrook[path] = brookName;
        if (!isExplicit)
        {
            AutoDerivedPaths[path] = true;
        }
    }

    /// <inheritdoc />
    public bool TryGetBrookName(
        string path,
        out string? brookName
    )
    {
        ArgumentNullException.ThrowIfNull(path);
        return PathToBrook.TryGetValue(path, out brookName);
    }

    /// <inheritdoc />
    public void ValidateExplicitPaths()
    {
        if (AutoDerivedPaths.IsEmpty)
        {
            return;
        }

        string[] autoDerived = AutoDerivedPaths.Keys.OrderBy(p => p, StringComparer.Ordinal).ToArray();
        throw new InvalidOperationException(
            $"RequireExplicitProjectionPaths is enabled but the following projection paths were auto-derived: " +
            $"{string.Join(", ", autoDerived)}. " +
            $"Set Path explicitly on [GenerateProjectionEndpoints(Path = \"...\")] for each projection.");
    }
}