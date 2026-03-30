using System;
using System.Globalization;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Raised when source state is not yet available at the requested source position.
/// </summary>
internal sealed class ReplicaSinkSourceStateUnavailableException : InvalidOperationException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkSourceStateUnavailableException" /> class.
    /// </summary>
    public ReplicaSinkSourceStateUnavailableException()
    {
        EntityId = string.Empty;
        ProjectionType = typeof(object);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkSourceStateUnavailableException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ReplicaSinkSourceStateUnavailableException(
        string message
    )
        : base(message)
    {
        EntityId = string.Empty;
        ProjectionType = typeof(object);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkSourceStateUnavailableException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ReplicaSinkSourceStateUnavailableException(
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        EntityId = string.Empty;
        ProjectionType = typeof(object);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkSourceStateUnavailableException" /> class.
    /// </summary>
    /// <param name="projectionType">The projection type being read.</param>
    /// <param name="entityId">The entity identifier being read.</param>
    /// <param name="requestedSourcePosition">The requested source position.</param>
    /// <param name="latestAvailableSourcePosition">The latest available source position, if known.</param>
    public ReplicaSinkSourceStateUnavailableException(
        Type projectionType,
        string entityId,
        long requestedSourcePosition,
        long? latestAvailableSourcePosition
    )
        : base(
            string.Format(
                CultureInfo.InvariantCulture,
                "Projection source state for '{0}' entity '{1}' is not yet available at requested source position '{2}'. Latest available position: '{3}'.",
                projectionType.Name,
                entityId,
                requestedSourcePosition,
                latestAvailableSourcePosition?.ToString(CultureInfo.InvariantCulture) ?? "<none>"))
    {
        ProjectionType = projectionType ?? throw new ArgumentNullException(nameof(projectionType));
        EntityId = entityId ?? throw new ArgumentNullException(nameof(entityId));
        RequestedSourcePosition = requestedSourcePosition;
        LatestAvailableSourcePosition = latestAvailableSourcePosition;
    }

    /// <summary>
    ///     Gets the entity identifier being read.
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    ///     Gets the latest available source position, if known.
    /// </summary>
    public long? LatestAvailableSourcePosition { get; }

    /// <summary>
    ///     Gets the projection type being read.
    /// </summary>
    public Type ProjectionType { get; }

    /// <summary>
    ///     Gets the requested source position.
    /// </summary>
    public long RequestedSourcePosition { get; }
}
