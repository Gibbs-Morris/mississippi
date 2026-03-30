using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Describes a named replica sink registration contributed by a provider package.
/// </summary>
public sealed class ReplicaSinkRegistrationDescriptor
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkRegistrationDescriptor" /> class.
    /// </summary>
    /// <param name="sinkKey">The named sink registration key.</param>
    /// <param name="clientKey">The keyed client or dependency key used by the provider.</param>
    /// <param name="format">The informational provider format identifier.</param>
    /// <param name="providerType">The provider implementation type.</param>
    /// <param name="provisioningMode">The configured provisioning mode.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="sinkKey" />, <paramref name="clientKey" />, <paramref name="format" />, or
    ///     <paramref name="providerType" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="sinkKey" />, <paramref name="clientKey" />, or <paramref name="format" /> is empty or
    ///     whitespace.
    /// </exception>
    public ReplicaSinkRegistrationDescriptor(
        string sinkKey,
        string clientKey,
        string format,
        Type providerType,
        ReplicaProvisioningMode provisioningMode
    )
    {
        ArgumentNullException.ThrowIfNull(sinkKey);
        ArgumentNullException.ThrowIfNull(clientKey);
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(providerType);
        ArgumentException.ThrowIfNullOrWhiteSpace(sinkKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        SinkKey = sinkKey;
        ClientKey = clientKey;
        Format = format;
        ProviderType = providerType;
        ProvisioningMode = provisioningMode;
    }

    /// <summary>
    ///     Gets the keyed client or dependency key used by the provider.
    /// </summary>
    public string ClientKey { get; }

    /// <summary>
    ///     Gets the informational provider format identifier.
    /// </summary>
    public string Format { get; }

    /// <summary>
    ///     Gets the provider type contributing the registration.
    /// </summary>
    public Type ProviderType { get; }

    /// <summary>
    ///     Gets the configured provisioning mode.
    /// </summary>
    public ReplicaProvisioningMode ProvisioningMode { get; }

    /// <summary>
    ///     Gets the named sink registration key.
    /// </summary>
    public string SinkKey { get; }
}