using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Inlet.Blazor.WebAssembly.ActionEffects;

/// <summary>
///     Interface for fetching projection data from the server.
/// </summary>
/// <remarks>
///     Implementations are responsible for retrieving projection data via HTTP
///     or other transport mechanisms. The fetcher is invoked by <see cref="InletSignalRActionEffect" />
///     when a subscription is created or when a projection update notification is received.
/// </remarks>
public interface IProjectionFetcher
{
    /// <summary>
    ///     Fetches the latest projection data for the specified projection type and entity.
    /// </summary>
    /// <param name="projectionType">The type of projection to fetch.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    ///     The fetch result containing the projection data and version,
    ///     or null if the projection type is not supported.
    /// </returns>
    Task<ProjectionFetchResult?> FetchAsync(
        Type projectionType,
        string entityId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Fetches projection data at a specific version for the specified projection type and entity.
    /// </summary>
    /// <param name="projectionType">The type of projection to fetch.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="version">The specific version to fetch.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    ///     The fetch result containing the projection data at the specified version,
    ///     or null if the projection type is not supported or the version does not exist.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method enables more efficient fetching when the exact version is known,
    ///         such as when receiving update notifications. Versioned endpoints are immutable
    ///         and can benefit from aggressive HTTP caching.
    ///     </para>
    ///     <para>
    ///         The default implementation falls back to <see cref="FetchAsync" /> for
    ///         backward compatibility with existing implementations.
    ///     </para>
    /// </remarks>
    Task<ProjectionFetchResult?> FetchAtVersionAsync(
        Type projectionType,
        string entityId,
        long version,
        CancellationToken cancellationToken
    ) =>
        FetchAsync(projectionType, entityId, cancellationToken);
}