using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Options;

using Mississippi.Brooks.Serialization.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Startup;

/// <summary>
///     Resolves the single snapshot payload serializer configured for Blob snapshot storage.
/// </summary>
internal sealed class SnapshotPayloadSerializerResolver
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotPayloadSerializerResolver" /> class.
    /// </summary>
    /// <param name="serializationProviders">The registered serialization providers.</param>
    /// <param name="options">The Blob snapshot storage options.</param>
    public SnapshotPayloadSerializerResolver(
        IEnumerable<ISerializationProvider> serializationProviders,
        IOptions<SnapshotBlobStorageOptions> options
    )
    {
        SerializationProviders = serializationProviders?.ToArray() ??
                                 throw new ArgumentNullException(nameof(serializationProviders));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    private IOptions<SnapshotBlobStorageOptions> Options { get; }

    private IReadOnlyCollection<ISerializationProvider> SerializationProviders { get; }

    /// <summary>
    ///     Gets the persisted serializer identity for the supplied provider.
    /// </summary>
    /// <param name="provider">The serialization provider.</param>
    /// <returns>The concrete persisted serializer identity.</returns>
    internal static string GetSerializerId(
        ISerializationProvider provider
    )
    {
        ArgumentNullException.ThrowIfNull(provider);
        Type providerType = provider.GetType();
        return providerType.FullName ?? providerType.Name;
    }

    /// <summary>
    ///     Resolves the single serializer matching the configured payload format.
    /// </summary>
    /// <returns>The resolved serialization provider.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when zero or multiple providers match the configured serializer format.
    /// </exception>
    public ISerializationProvider ResolveConfiguredSerializer()
    {
        string configuredFormat = Options.Value.PayloadSerializerFormat;
        List<ISerializationProvider> matches = SerializationProviders.Where(provider =>
                string.Equals(provider.Format, configuredFormat, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException(
                $"No ISerializationProvider is registered for payload serializer format '{configuredFormat}'. Register exactly one matching provider before startup."),
            var _ => throw new InvalidOperationException(
                $"Multiple ISerializationProvider registrations match payload serializer format '{configuredFormat}'. Register exactly one matching provider before startup."),
        };
    }

    /// <summary>
    ///     Resolves the configured serializer and its persisted concrete serializer identity.
    /// </summary>
    /// <returns>The resolved serializer descriptor.</returns>
    public SnapshotPayloadSerializerDescriptor ResolveConfiguredSerializerDescriptor()
    {
        ISerializationProvider provider = ResolveConfiguredSerializer();
        return new(provider, GetSerializerId(provider));
    }
}