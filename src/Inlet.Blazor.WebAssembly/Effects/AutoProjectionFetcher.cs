using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Inlet.Blazor.WebAssembly.Effects;

/// <summary>
///     Generic projection fetcher that uses the <see cref="IProjectionDtoRegistry" />
///     to automatically resolve DTO types and fetch projections via HTTP.
/// </summary>
/// <remarks>
///     <para>
///         This fetcher eliminates the need for manual if/else mapping per projection type.
///         It uses the registry to:
///         <list type="bullet">
///             <item>Look up the route for a given DTO type.</item>
///             <item>Construct the HTTP endpoint URL.</item>
///             <item>Deserialize the response to the correct DTO type.</item>
///         </list>
///     </para>
///     <para>
///         The fetcher also extracts version information from the ETag header
///         returned by the server, enabling optimistic concurrency.
///     </para>
/// </remarks>
public sealed class AutoProjectionFetcher : IProjectionFetcher
{
    private const string DefaultRoutePrefix = "/api/projections";

    /// <summary>
    ///     Initializes a new instance of the <see cref="AutoProjectionFetcher" /> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for fetching projections.</param>
    /// <param name="registry">The projection DTO registry.</param>
    /// <param name="routePrefix">
    ///     Optional route prefix. Defaults to <c>/api/projections</c>.
    /// </param>
    public AutoProjectionFetcher(
        HttpClient httpClient,
        IProjectionDtoRegistry registry,
        string? routePrefix = null
    )
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(registry);
        Http = httpClient;
        Registry = registry;
        RoutePrefix = routePrefix ?? DefaultRoutePrefix;
    }

    private HttpClient Http { get; }

    private IProjectionDtoRegistry Registry { get; }

    private string RoutePrefix { get; }

    /// <inheritdoc />
    public async Task<ProjectionFetchResult?> FetchAsync(
        Type projectionType,
        string entityId,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        // Look up the path for this DTO type
        if (!Registry.TryGetPath(projectionType, out string? path) || path is null)
        {
            // Type not registered - unsupported projection
            return null;
        }

        // Construct the endpoint URL for latest version
        string url = $"{RoutePrefix}/{path}/{Uri.EscapeDataString(entityId)}";
        return await FetchFromUrlAsync(projectionType, url, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProjectionFetchResult?> FetchAtVersionAsync(
        Type projectionType,
        string entityId,
        long version,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        // Look up the path for this DTO type
        if (!Registry.TryGetPath(projectionType, out string? path) || path is null)
        {
            // Type not registered - unsupported projection
            return null;
        }

        // Construct the endpoint URL for specific version
        // Pattern: {RoutePrefix}/{path}/{entityId}/at/{version}
        string url = $"{RoutePrefix}/{path}/{Uri.EscapeDataString(entityId)}/at/{version}";
        return await FetchFromUrlAsync(projectionType, url, cancellationToken);
    }

    private async Task<ProjectionFetchResult?> FetchFromUrlAsync(
        Type projectionType,
        string url,
        CancellationToken cancellationToken
    )
    {
        // Fetch with response headers for ETag
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        using HttpResponseMessage response = await Http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // 404 means no events yet - return NotFound sentinel (not null).
            // The subscription is still valid and will receive SignalR updates.
            return ProjectionFetchResult.NotFound;
        }

        response.EnsureSuccessStatusCode();

        // Extract version from ETag header
        long version = 0;
        if (response.Headers.ETag?.Tag is { } etag)
        {
            // ETag format is "\"123\"" - extract the number
            string trimmed = etag.Trim('"');
            _ = long.TryParse(trimmed, out version);
        }

        // Deserialize to the registered DTO type
        object? data = await response.Content.ReadFromJsonAsync(projectionType, cancellationToken);
        if (data is null)
        {
            return null;
        }

        return ProjectionFetchResult.Create(data, version);
    }
}