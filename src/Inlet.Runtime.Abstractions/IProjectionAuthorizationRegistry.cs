using System.Collections.Generic;


namespace Mississippi.Inlet.Runtime.Abstractions;

/// <summary>
///     Registry that maps projection paths to resolved authorization metadata.
/// </summary>
public interface IProjectionAuthorizationRegistry
{
    /// <summary>
    ///     Gets all registered projection paths that have authorization metadata.
    /// </summary>
    /// <returns>An enumerable of all registered projection paths.</returns>
    IReadOnlyCollection<string> GetAllPaths();

    /// <summary>
    ///     Gets authorization metadata for the projection path.
    /// </summary>
    /// <param name="path">The projection path.</param>
    /// <returns>The authorization metadata if registered; otherwise, <see langword="null" />.</returns>
    ProjectionAuthorizationMetadata? GetAuthorizationMetadata(
        string path
    );

    /// <summary>
    ///     Registers authorization metadata for a projection path.
    /// </summary>
    /// <param name="path">The projection path.</param>
    /// <param name="metadata">The authorization metadata.</param>
    void Register(
        string path,
        ProjectionAuthorizationMetadata metadata
    );
}