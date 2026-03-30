using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Describes the observable state of a replica destination.
/// </summary>
public sealed class ReplicaTargetInspection
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaTargetInspection" /> class.
    /// </summary>
    /// <param name="destinationIdentity">The provider-facing destination identity.</param>
    /// <param name="targetExists">A value indicating whether the target exists.</param>
    /// <param name="writeCount">The number of writes observed by the provider.</param>
    /// <param name="latestSourcePosition">The latest applied source position.</param>
    /// <param name="latestPayload">The latest applied payload.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="destinationIdentity" /> is null.</exception>
    public ReplicaTargetInspection(
        ReplicaDestinationIdentity destinationIdentity,
        bool targetExists,
        long writeCount,
        long? latestSourcePosition = null,
        object? latestPayload = null
    )
    {
        ArgumentNullException.ThrowIfNull(destinationIdentity);
        DestinationIdentity = destinationIdentity;
        TargetExists = targetExists;
        WriteCount = writeCount;
        LatestSourcePosition = latestSourcePosition;
        LatestPayload = latestPayload;
    }

    /// <summary>
    ///     Gets the provider-facing destination identity.
    /// </summary>
    public ReplicaDestinationIdentity DestinationIdentity { get; }

    /// <summary>
    ///     Gets the latest applied payload.
    /// </summary>
    public object? LatestPayload { get; }

    /// <summary>
    ///     Gets the latest applied source position.
    /// </summary>
    public long? LatestSourcePosition { get; }

    /// <summary>
    ///     Gets a value indicating whether the target exists.
    /// </summary>
    public bool TargetExists { get; }

    /// <summary>
    ///     Gets the number of writes observed by the provider.
    /// </summary>
    public long WriteCount { get; }
}