using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Microsoft.AspNetCore.SignalR;


namespace Mississippi.Aqueduct;

/// <summary>
///     Thread-safe registry for tracking local SignalR hub connections.
/// </summary>
/// <remarks>
///     <para>
///         This implementation uses a <see cref="ConcurrentDictionary{TKey,TValue}" />
///         to provide thread-safe access to hub connection contexts. It is intended
///         for use within a single server instance to track connections that are
///         hosted locally.
///     </para>
/// </remarks>
internal sealed class ConnectionRegistry : IConnectionRegistry
{
    private readonly ConcurrentDictionary<string, HubConnectionContext> connections = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public int Count => connections.Count;

    /// <inheritdoc />
    public IEnumerable<HubConnectionContext> GetAll() => connections.Values;

    /// <inheritdoc />
    public HubConnectionContext? GetConnection(
        string connectionId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionId);
        return connections.TryGetValue(connectionId, out HubConnectionContext? connection) ? connection : null;
    }

    /// <inheritdoc />
    public bool TryAdd(
        string connectionId,
        HubConnectionContext connection
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionId);
        ArgumentNullException.ThrowIfNull(connection);
        return connections.TryAdd(connectionId, connection);
    }

    /// <inheritdoc />
    public bool TryRemove(
        string connectionId
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionId);
        return connections.TryRemove(connectionId, out HubConnectionContext _);
    }
}