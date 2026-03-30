using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Bootstrap;

/// <summary>
///     Defines bootstrap-provider options for replica sink onboarding proofs.
/// </summary>
public sealed class BootstrapReplicaSinkOptions : ReplicaSinkRegistrationOptions
{
    /// <summary>
    ///     Gets or sets a value indicating whether the provider should behave as an in-memory ephemeral target.
    /// </summary>
    public bool IsEphemeral { get; set; } = true;
}