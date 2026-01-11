using System;


namespace Mississippi.Inlet.Blazor.WebAssembly.Effects;

/// <summary>
///     Represents the result of fetching a projection.
/// </summary>
/// <param name="Data">The projection data object.</param>
/// <param name="Version">The version/etag of the projection for optimistic concurrency.</param>
public sealed record ProjectionFetchResult(object Data, long Version)
{
    /// <summary>
    ///     Creates a typed fetch result.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="data">The projection data.</param>
    /// <param name="version">The projection version.</param>
    /// <returns>A new fetch result.</returns>
    public static ProjectionFetchResult Create<T>(T data, long version)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(data);
        return new ProjectionFetchResult(data, version);
    }
}
