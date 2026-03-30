using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;

/// <summary>
///     Identifies a provider-facing destination independently of runtime delivery identity.
/// </summary>
public sealed class ReplicaDestinationIdentity : IEquatable<ReplicaDestinationIdentity>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaDestinationIdentity" /> class.
    /// </summary>
    /// <param name="clientKey">The keyed client or dependency key used by the provider.</param>
    /// <param name="targetName">The provider-neutral destination name.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="clientKey" /> or <paramref name="targetName" /> is
    ///     null.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="clientKey" /> or <paramref name="targetName" /> is
    ///     empty or whitespace.
    /// </exception>
    public ReplicaDestinationIdentity(
        string clientKey,
        string targetName
    )
    {
        ArgumentNullException.ThrowIfNull(clientKey);
        ArgumentNullException.ThrowIfNull(targetName);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetName);
        ClientKey = clientKey;
        TargetName = targetName;
    }

    /// <summary>
    ///     Gets the keyed client or dependency key used by the provider.
    /// </summary>
    public string ClientKey { get; }

    /// <summary>
    ///     Gets the provider-neutral destination name.
    /// </summary>
    public string TargetName { get; }

    /// <inheritdoc />
    public bool Equals(
        ReplicaDestinationIdentity? other
    ) =>
        other is not null &&
        string.Equals(ClientKey, other.ClientKey, StringComparison.Ordinal) &&
        string.Equals(TargetName, other.TargetName, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(
        object? obj
    ) =>
        obj is ReplicaDestinationIdentity other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(ClientKey, TargetName);

    /// <inheritdoc />
    public override string ToString() => $"{ClientKey}:{TargetName}";
}