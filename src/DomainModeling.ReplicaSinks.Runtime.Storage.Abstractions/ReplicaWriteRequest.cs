using System;

using Mississippi.DomainModeling.ReplicaSinks.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Represents a single provider-facing replica write attempt.
/// </summary>
public sealed class ReplicaWriteRequest
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaWriteRequest" /> class.
    /// </summary>
    /// <param name="target">The target receiving the write.</param>
    /// <param name="deliveryKey">The logical delivery key for the replica item.</param>
    /// <param name="sourcePosition">The source position represented by the payload.</param>
    /// <param name="writeMode">The requested write mode.</param>
    /// <param name="contractIdentity">The stable replica contract identity.</param>
    /// <param name="payload">The materialized payload to publish.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="target" />, <paramref name="deliveryKey" />, or
    ///     <paramref name="contractIdentity" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="deliveryKey" /> or <paramref name="contractIdentity" />
    ///     is empty or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sourcePosition" /> is negative.</exception>
    public ReplicaWriteRequest(
        ReplicaTargetDescriptor target,
        string deliveryKey,
        long sourcePosition,
        ReplicaWriteMode writeMode,
        string contractIdentity,
        object? payload
    )
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(deliveryKey);
        ArgumentNullException.ThrowIfNull(contractIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contractIdentity);
        ArgumentOutOfRangeException.ThrowIfLessThan(sourcePosition, 0L);
        Target = target;
        DeliveryKey = deliveryKey;
        SourcePosition = sourcePosition;
        WriteMode = writeMode;
        ContractIdentity = contractIdentity;
        Payload = payload;
    }

    /// <summary>
    ///     Gets the stable replica contract identity.
    /// </summary>
    public string ContractIdentity { get; }

    /// <summary>
    ///     Gets the logical delivery key for the replica item.
    /// </summary>
    public string DeliveryKey { get; }

    /// <summary>
    ///     Gets a value indicating whether the request represents a delete or tombstone operation.
    /// </summary>
    public bool IsDeleted { get; init; }

    /// <summary>
    ///     Gets the materialized payload to publish.
    /// </summary>
    public object? Payload { get; }

    /// <summary>
    ///     Gets the source position represented by the payload.
    /// </summary>
    public long SourcePosition { get; }

    /// <summary>
    ///     Gets the target receiving the write.
    /// </summary>
    public ReplicaTargetDescriptor Target { get; }

    /// <summary>
    ///     Gets the requested write mode.
    /// </summary>
    public ReplicaWriteMode WriteMode { get; }
}