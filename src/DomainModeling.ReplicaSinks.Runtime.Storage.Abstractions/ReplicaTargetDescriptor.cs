using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Describes the destination a replica sink provider should validate or provision.
/// </summary>
public sealed class ReplicaTargetDescriptor
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaTargetDescriptor" /> class.
    /// </summary>
    /// <param name="destinationIdentity">The provider-facing destination identity.</param>
    /// <param name="provisioningMode">The provisioning mode to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="destinationIdentity" /> is null.</exception>
    public ReplicaTargetDescriptor(
        ReplicaDestinationIdentity destinationIdentity,
        ReplicaProvisioningMode provisioningMode = ReplicaProvisioningMode.ValidateOnly
    )
    {
        ArgumentNullException.ThrowIfNull(destinationIdentity);
        DestinationIdentity = destinationIdentity;
        ProvisioningMode = provisioningMode;
    }

    /// <summary>
    ///     Gets the provider-facing destination identity.
    /// </summary>
    public ReplicaDestinationIdentity DestinationIdentity { get; }

    /// <summary>
    ///     Gets the provisioning mode to apply.
    /// </summary>
    public ReplicaProvisioningMode ProvisioningMode { get; }
}