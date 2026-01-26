using System;


namespace Mississippi.Inlet.Client.Abstractions;

/// <summary>
///     Registry for projection types and their associated paths.
/// </summary>
public interface IProjectionRegistry
{
    /// <summary>
    ///     Gets the path for a projection type.
    /// </summary>
    /// <param name="projectionType">The projection type.</param>
    /// <returns>The path for the projection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="projectionType" /> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no path is registered for the type.</exception>
    string GetPath(
        Type projectionType
    );

    /// <summary>
    ///     Gets whether a projection type is registered.
    /// </summary>
    /// <param name="projectionType">The projection type.</param>
    /// <returns>True if the projection type has a registered path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="projectionType" /> is null.</exception>
    bool IsRegistered(
        Type projectionType
    );

    /// <summary>
    ///     Registers a path for a projection type.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="path">The projection path.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path" /> is null.</exception>
    void Register<T>(
        string path
    )
        where T : class;
}