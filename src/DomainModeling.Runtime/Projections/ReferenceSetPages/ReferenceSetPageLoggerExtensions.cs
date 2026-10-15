using System;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Logging;


namespace Mississippi.DomainModeling.Runtime.Projections.ReferenceSetPages;

/// <summary>Provides source-generated structured diagnostics for bounded membership reads.</summary>
internal static partial class ReferenceSetPageLoggerExtensions
{
    /// <summary>Creates a stable masked identity for diagnostics without logging the raw entity ID.</summary>
    /// <param name="entityId">The validated collection entity ID.</param>
    /// <returns>The SHA-256 hexadecimal entity identity.</returns>
    public static string GetEntityHash(
        string entityId
    ) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(entityId)));

    /// <summary>Records preparation of an immutable membership page cache.</summary>
    /// <param name="logger">The typed logger.</param>
    /// <param name="entityHash">The masked collection entity identity.</param>
    /// <param name="projectionType">The membership projection type.</param>
    /// <param name="version">The immutable collection version.</param>
    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Collection {EntityHash}: Activating membership page cache for {ProjectionType} at version {Version}")]
    public static partial void PageCacheActivating(
        this ILogger logger,
        string entityHash,
        string projectionType,
        long version
    );

    /// <summary>Records the end of an immutable page-cache activation.</summary>
    /// <param name="logger">The typed logger.</param>
    /// <param name="entityHash">The masked collection identity.</param>
    /// <param name="version">The immutable collection version.</param>
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Collection {EntityHash}: Deactivated membership page cache at version {Version}")]
    public static partial void PageCacheDeactivated(
        this ILogger logger,
        string entityHash,
        long version
    );

    /// <summary>Records preparation of the ordinal membership index.</summary>
    /// <param name="logger">The typed logger.</param>
    /// <param name="entityHash">The masked collection identity.</param>
    /// <param name="version">The immutable collection version.</param>
    /// <param name="count">The indexed membership count.</param>
    /// <param name="elapsedMilliseconds">The preparation duration.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message =
            "Collection {EntityHash}: Prepared ordinal membership index at version {Version} with {Count} entries in {ElapsedMilliseconds} ms")]
    public static partial void PageIndexPrepared(
        this ILogger logger,
        string entityHash,
        long version,
        int count,
        double elapsedMilliseconds
    );

    /// <summary>Records a completed bounded membership read.</summary>
    /// <param name="logger">The typed logger.</param>
    /// <param name="entityHash">The masked collection identity.</param>
    /// <param name="version">The actual collection version.</param>
    /// <param name="count">The returned reference count.</param>
    /// <param name="elapsedMilliseconds">The read duration.</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Collection {EntityHash}: Read membership page at version {Version} with {Count} references in {ElapsedMilliseconds} ms")]
    public static partial void PageReadCompleted(
        this ILogger logger,
        string entityHash,
        long version,
        int count,
        double elapsedMilliseconds
    );

    /// <summary>Records the caller's requested membership selection.</summary>
    /// <param name="logger">The typed logger.</param>
    /// <param name="entityHash">The masked collection identity.</param>
    /// <param name="projectionType">The named projection type.</param>
    /// <param name="offset">The requested offset.</param>
    /// <param name="limit">The requested maximum reference count.</param>
    /// <param name="version">The pinned version, or null for a latest read.</param>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Collection {EntityHash}: Reading membership page for {ProjectionType}, offset {Offset}, limit {Limit}, version {Version}")]
    public static partial void PageReadStarted(
        this ILogger logger,
        string entityHash,
        string projectionType,
        int offset,
        int limit,
        long? version
    );
}