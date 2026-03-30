namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Defines common registration options shared by replica sink providers.
/// </summary>
public class ReplicaSinkRegistrationOptions
{
    /// <summary>
    ///     Gets or sets the client key used to resolve provider-owned keyed dependencies.
    /// </summary>
    public string ClientKey { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the provisioning mode applied during onboarding.
    /// </summary>
    public ReplicaProvisioningMode ProvisioningMode { get; set; } = ReplicaProvisioningMode.ValidateOnly;
}