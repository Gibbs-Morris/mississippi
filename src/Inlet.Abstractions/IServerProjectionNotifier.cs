using System;


namespace Mississippi.Inlet.Abstractions;

/// <summary>
///     Provides notifications when projections are updated (server-side interface).
/// </summary>
/// <remarks>
///     <para>
///         This interface abstracts the notification mechanism, allowing different
///         implementations for Blazor Server (in-process) and SignalR-based systems.
///     </para>
/// </remarks>
public interface IServerProjectionNotifier
{
    /// <summary>
    ///     Notifies all subscribers that a projection has been updated.
    /// </summary>
    /// <param name="projectionType">The type of projection that changed.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="newVersion">The new version number.</param>
    void NotifyProjectionChanged(
        string projectionType,
        string entityId,
        long newVersion
    );

    /// <summary>
    ///     Subscribes to updates for a specific projection.
    /// </summary>
    /// <param name="projectionType">The type name of the projection.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="callback">The callback to invoke when the projection is updated.</param>
    /// <returns>A disposable that unsubscribes when disposed.</returns>
    IDisposable Subscribe(
        string projectionType,
        string entityId,
        Action<ProjectionUpdatedEvent> callback
    );
}

/// <summary>
///     Data for projection update notifications.
/// </summary>
/// <param name="ProjectionType">The type name of the projection that was updated.</param>
/// <param name="EntityId">The entity identifier.</param>
/// <param name="NewVersion">The new version of the projection.</param>
public sealed record ProjectionUpdatedEvent(string ProjectionType, string EntityId, long NewVersion);