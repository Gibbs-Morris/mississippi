namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Defines how a replica sink provider should treat target provisioning during startup or onboarding.
/// </summary>
public enum ReplicaProvisioningMode
{
    /// <summary>
    ///     Validates an existing target without creating it.
    /// </summary>
    ValidateOnly = 0,

    /// <summary>
    ///     Creates the target when it does not already exist.
    /// </summary>
    CreateIfMissing = 1,
}