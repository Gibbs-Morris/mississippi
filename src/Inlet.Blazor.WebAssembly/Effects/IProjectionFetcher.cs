using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Inlet.Blazor.WebAssembly.Effects;

/// <summary>
///     Interface for fetching projection data from the server.
/// </summary>
/// <remarks>
///     Implementations are responsible for retrieving projection data via HTTP
///     or other transport mechanisms. The fetcher is invoked by <see cref="InletSignalREffect" />
///     when a subscription is created or when a projection update notification is received.
/// </remarks>
public interface IProjectionFetcher
{
    /// <summary>
    ///     Fetches projection data for the specified projection type and entity.
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
}