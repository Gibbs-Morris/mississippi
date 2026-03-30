using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Represents the terminal outcome returned by a replica sink provider.
/// </summary>
public sealed class ReplicaWriteResult
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaWriteResult" /> class.
    /// </summary>
    /// <param name="outcome">The terminal write outcome.</param>
    /// <param name="destinationIdentity">The provider-facing destination identity.</param>
    /// <param name="sourcePosition">The source position evaluated by the provider.</param>
    /// <param name="details">Optional provider details.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="destinationIdentity" /> is null.</exception>
    public ReplicaWriteResult(
        ReplicaWriteOutcome outcome,
        ReplicaDestinationIdentity destinationIdentity,
        long sourcePosition,
        string? details = null
    )
    {
        ArgumentNullException.ThrowIfNull(destinationIdentity);
        Outcome = outcome;
        DestinationIdentity = destinationIdentity;
        SourcePosition = sourcePosition;
        Details = details;
    }

    /// <summary>
    ///     Gets the provider-facing destination identity.
    /// </summary>
    public ReplicaDestinationIdentity DestinationIdentity { get; }

    /// <summary>
    ///     Gets optional provider details.
    /// </summary>
    public string? Details { get; }

    /// <summary>
    ///     Gets the terminal write outcome.
    /// </summary>
    public ReplicaWriteOutcome Outcome { get; }

    /// <summary>
    ///     Gets the source position evaluated by the provider.
    /// </summary>
    public long SourcePosition { get; }
}