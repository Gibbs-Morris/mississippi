namespace Mississippi.Ripples.Client;

using Microsoft.Extensions.Logging;

/// <summary>
/// Logging extension methods for <see cref="ClientRipplePool{TProjection}"/>.
/// </summary>
internal static partial class ClientRipplePoolLoggerExtensions
{
    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "GetOrCreate {ProjectionType} entity {EntityId}")]
    public static partial void GetOrCreateRipple(
        ILogger logger,
        string projectionType,
        string entityId);

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Marked {ProjectionType} entity {EntityId} as hidden (warm)")]
    public static partial void MarkedHidden(
        ILogger logger,
        string projectionType,
        string entityId);

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Marked {ProjectionType} entity {EntityId} as visible (hot)")]
    public static partial void MarkedVisible(
        ILogger logger,
        string projectionType,
        string entityId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Prefetching {Count} {ProjectionType} projections")]
    public static partial void PrefetchingProjections(
        ILogger logger,
        string projectionType,
        int count);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Batch prefetch succeeded for {ProjectionType}, loaded {Count} projections")]
    public static partial void BatchPrefetchSucceeded(
        ILogger logger,
        string projectionType,
        int count);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Batch endpoint unavailable for {ProjectionType}, falling back to individual fetches")]
    public static partial void BatchEndpointUnavailable(
        ILogger logger,
        string projectionType);
}
