using System;


namespace Mississippi.Inlet.Projection.Abstractions;

/// <summary>
///     Marks a class as a projection that can be subscribed to via Inlet.
/// </summary>
/// <remarks>
///     <para>
///         This attribute provides a unified path-based addressing scheme for projections.
///         The same attribute can be applied to:
///         <list type="bullet">
///             <item>Server-side projection classes (Orleans grains)</item>
///             <item>Client-side DTO classes (Blazor WASM)</item>
///             <item>Shared contract classes</item>
///         </list>
///     </para>
///     <para>
///         The path follows a <c>{feature}/{module}</c> convention, for example:
///         <list type="bullet">
///             <item><c>cascade/channels</c> - Channel messages in the Cascade sample</item>
///             <item><c>inventory/products</c> - Product projections in an inventory system</item>
///         </list>
///     </para>
///     <para>
///         Full entity paths are constructed by appending the entity ID:
///         <c>{feature}/{module}/{entityId}</c>, for example <c>cascade/channels/abc-123</c>.
///     </para>
///     <para>
///         The path is used for:
///         <list type="bullet">
///             <item>API routes: <c>GET /api/projections/{path}/{entityId}</c></item>
///             <item>SignalR subscriptions: subscribe to <c>{path}</c> with entity ID</item>
///         </list>
///     </para>
///     <para>
///         Example usage:
///         <code>
///             // Server-side projection (in Domain project):
///             [ProjectionPath("cascade/channels")]
///             public sealed record ChannelMessagesProjection { ... }
/// 
///             // Client-side DTO (in Contracts project, WASM-safe):
///             [ProjectionPath("cascade/channels")]
///             public sealed record ChannelMessagesDto { ... }
///         </code>
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ProjectionPathAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionPathAttribute" /> class.
    /// </summary>
    /// <param name="path">
    ///     The base path for this projection in <c>{feature}/{module}</c> format.
    ///     Must not be null or empty.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="path" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="path" /> is empty or whitespace.
    /// </exception>
    public ProjectionPathAttribute(
        string path
    )
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = path;
    }

    /// <summary>
    ///     Gets the base path for this projection.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The path follows a <c>{feature}/{module}</c> convention.
    ///         Entity IDs are appended to form the full entity path.
    ///     </para>
    /// </remarks>
    public string Path { get; }

    /// <summary>
    ///     Constructs the full entity path by appending the entity ID.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The full path: <c>{path}/{entityId}</c>.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="entityId" /> is null.
    /// </exception>
    public string GetEntityPath(
        string entityId
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return $"{Path}/{entityId}";
    }

    /// <summary>
    ///     Constructs the full versioned entity path.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="version">The version number.</param>
    /// <returns>The full path: <c>{path}/{entityId}/{version}</c>.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="entityId" /> is null.
    /// </exception>
    public string GetVersionedEntityPath(
        string entityId,
        long version
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return $"{Path}/{entityId}/{version}";
    }
}