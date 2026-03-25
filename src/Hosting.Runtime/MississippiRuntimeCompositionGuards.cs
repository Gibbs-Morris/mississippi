using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Hosting.Runtime;

/// <summary>
///     Validates runtime-owned host composition before Orleans startup is finalized.
/// </summary>
internal static class MississippiRuntimeCompositionGuards
{
    private const string GatewayHostModeMarkerFullName = "Mississippi.Hosting.Gateway.MississippiGatewayHostModeMarker";

    private static readonly string[] FrozenOwnershipAssemblyPrefixes =
    [
        "Orleans.Persistence",
        "Orleans.Streaming",
    ];

    private static readonly string[] FrozenOwnershipMarkers =
    [
        "ClusterOptions",
        "Clustering",
        "EndpointOptions",
        "GatewayOptions",
        "GrainStorage",
        "StorageProvider",
        "StreamProvider",
        "PersistentStream",
        "QueueAdapter",
        "PubSub",
        "Streams",
    ];

    /// <summary>
    ///     Captures the current frozen Orleans ownership descriptor graph for later comparison.
    /// </summary>
    /// <param name="services">The silo service collection to inspect.</param>
    /// <returns>A count-based snapshot of ownership-sensitive descriptors.</returns>
    internal static IReadOnlyDictionary<string, int> CaptureFrozenOrleansOwnership(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.Where(IsFrozenOwnershipDescriptor)
            .GroupBy(CreateDescriptorSignature, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    }

    /// <summary>
    ///     Throws when Orleans callback replay mutates frozen provider, storage, clustering, or endpoint ownership.
    /// </summary>
    /// <param name="services">The silo service collection to inspect after callback replay.</param>
    /// <param name="frozenOwnership">The ownership snapshot captured before callback replay.</param>
    internal static void ThrowIfFrozenOrleansOwnershipChanged(
        IServiceCollection services,
        IReadOnlyDictionary<string, int> frozenOwnership
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(frozenOwnership);
        Dictionary<string, int> currentOwnership = CaptureFrozenOrleansOwnership(services)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        if (HaveEquivalentDescriptorCounts(frozenOwnership, currentOwnership))
        {
            return;
        }

        string categories = DescribeFrozenOwnershipCategories(frozenOwnership, currentOwnership);
        throw new InvalidOperationException(
            $"Mississippi runtime freezes Orleans {categories} ownership after AddMississippiRuntime(...). Remove the conflicting ownership change from MississippiRuntimeBuilder.Orleans(...) and keep only additive silo tuning that preserves the Mississippi-managed runtime attachment.");
    }

    /// <summary>
    ///     Throws when unsupported same-host or competing Orleans ownership markers are present.
    /// </summary>
    /// <param name="services">The service collection to inspect.</param>
    internal static void ThrowIfUnsupportedCompositionExists(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        if (services.Any(IsGatewayHostModeMarker))
        {
            throw new InvalidOperationException(
                "Mississippi runtime and gateway composition cannot share the same host in this rollout. Remove the gateway host composition from this process and keep the runtime host on the supported runtime-only path.");
        }

        if (services.Any(descriptor => descriptor.ServiceType == typeof(MississippiCompetingOrleansOwnershipMarker)))
        {
            throw new InvalidOperationException(
                "Mississippi runtime owns the top-level Orleans silo attachment after AddMississippiRuntime(...). Remove the competing Orleans host attachment and compose supported silo changes through MississippiRuntimeBuilder.Orleans(...).");
        }
    }

    private static string CreateDescriptorSignature(
        ServiceDescriptor descriptor
    )
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        string serviceType = descriptor.ServiceType.AssemblyQualifiedName ??
                             descriptor.ServiceType.FullName ?? descriptor.ServiceType.Name;
        string implementation = descriptor.ImplementationType?.AssemblyQualifiedName ??
                                descriptor.ImplementationInstance?.GetType().AssemblyQualifiedName ??
                                descriptor.ImplementationFactory?.Method.ToString() ?? "null";
        return string.Concat(serviceType, "|", descriptor.Lifetime, "|", implementation);
    }

    private static string DescribeFrozenOwnershipCategories(
        IReadOnlyDictionary<string, int> expected,
        Dictionary<string, int> actual
    )
    {
        HashSet<string> categories = [];
        foreach (string signature in expected.Keys.Concat(actual.Keys).Distinct(StringComparer.Ordinal))
        {
            expected.TryGetValue(signature, out int expectedCount);
            actual.TryGetValue(signature, out int actualCount);
            if (expectedCount == actualCount)
            {
                continue;
            }

            if (signature.Contains("ClusterOptions", StringComparison.Ordinal) ||
                signature.Contains("Clustering", StringComparison.Ordinal))
            {
                categories.Add("clustering");
            }

            if (signature.Contains("EndpointOptions", StringComparison.Ordinal) ||
                signature.Contains("GatewayOptions", StringComparison.Ordinal))
            {
                categories.Add("endpoint");
            }

            if (signature.Contains("GrainStorage", StringComparison.Ordinal) ||
                signature.Contains("StorageProvider", StringComparison.Ordinal) ||
                signature.Contains("Orleans.Persistence", StringComparison.Ordinal))
            {
                categories.Add("storage");
            }

            if (signature.Contains("StreamProvider", StringComparison.Ordinal) ||
                signature.Contains("PersistentStream", StringComparison.Ordinal) ||
                signature.Contains("QueueAdapter", StringComparison.Ordinal) ||
                signature.Contains("PubSub", StringComparison.Ordinal) ||
                signature.Contains("Streams", StringComparison.Ordinal) ||
                signature.Contains("Orleans.Streaming", StringComparison.Ordinal))
            {
                categories.Add("provider");
            }
        }

        return categories.Count switch
        {
            0 => "provider, storage, clustering, and endpoint",
            1 => categories.Single(),
            2 => string.Join(" and ", categories.OrderBy(category => category, StringComparer.Ordinal)),
            var _ => string.Join(
                         ", ",
                         categories.OrderBy(category => category, StringComparer.Ordinal).Take(categories.Count - 1)) +
                     ", and " +
                     categories.OrderBy(category => category, StringComparer.Ordinal).Last(),
        };
    }

    private static bool HaveEquivalentDescriptorCounts(
        IReadOnlyDictionary<string, int> expected,
        Dictionary<string, int> actual
    )
    {
        if (expected.Count != actual.Count)
        {
            return false;
        }

        foreach ((string key, int value) in expected)
        {
            if (!actual.TryGetValue(key, out int currentValue) || (currentValue != value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsFrozenOwnershipDescriptor(
        ServiceDescriptor descriptor
    )
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        string signature = CreateDescriptorSignature(descriptor);
        return FrozenOwnershipMarkers.Any(marker => signature.Contains(marker, StringComparison.Ordinal)) ||
               FrozenOwnershipAssemblyPrefixes.Any(prefix => signature.Contains(prefix, StringComparison.Ordinal));
    }

    private static bool IsGatewayHostModeMarker(
        ServiceDescriptor descriptor
    )
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return (descriptor.ServiceType == typeof(MississippiGatewayHostModeMarker)) ||
               string.Equals(descriptor.ServiceType.FullName, GatewayHostModeMarkerFullName, StringComparison.Ordinal);
    }
}