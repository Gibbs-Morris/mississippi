using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Identifies a single durable delivery lane for one binding/entity pair.
/// </summary>
internal readonly record struct ReplicaSinkDeliveryIdentity
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkDeliveryIdentity" /> struct.
    /// </summary>
    /// <param name="bindingIdentity">The immutable binding identity.</param>
    /// <param name="entityId">The replicated entity identifier.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityId" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="entityId" /> is empty or whitespace.</exception>
    public ReplicaSinkDeliveryIdentity(
        ReplicaSinkBindingIdentity bindingIdentity,
        string entityId
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        BindingIdentity = bindingIdentity;
        EntityId = entityId;
    }

    /// <summary>
    ///     Gets the immutable binding identity.
    /// </summary>
    public ReplicaSinkBindingIdentity BindingIdentity { get; }

    /// <summary>
    ///     Gets the runtime-owned delivery key.
    /// </summary>
    public string DeliveryKey =>
        $"{BindingIdentity.ProjectionTypeName}::{BindingIdentity.SinkKey}::{BindingIdentity.TargetName}::{EntityId}";

    /// <summary>
    ///     Gets the replicated entity identifier.
    /// </summary>
    public string EntityId { get; }
}