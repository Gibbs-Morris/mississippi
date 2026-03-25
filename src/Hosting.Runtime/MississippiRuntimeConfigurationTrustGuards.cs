using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;


namespace Mississippi.Hosting.Runtime;

/// <summary>
///     Validates runtime configuration trust boundaries before the host can contact external infrastructure.
/// </summary>
internal static class MississippiRuntimeConfigurationTrustGuards
{
    private enum MississippiRuntimeEndpointHostClass
    {
        Local,

        External,
    }

    private static readonly string[] ConnectionStringEndpointKeys =
    [
        "AccountEndpoint",
        "BlobEndpoint",
        "TableEndpoint",
        "QueueEndpoint",
        "FileEndpoint",
        "Endpoint",
        "ServiceUri",
    ];

    private static readonly string[] DefaultAllowedExternalSchemes = ["https"];

    private static readonly string[] EndpointValueKeySuffixes =
    [
        ":Endpoint",
        ":ServiceUri",
        ":AccountEndpoint",
        ":BlobEndpoint",
        ":TableEndpoint",
        ":QueueEndpoint",
        ":FileEndpoint",
    ];

    private static readonly string[] StorageDefaultEndpointPrefixes =
    [
        "blob",
        "queue",
        "table",
        "file",
    ];

    /// <summary>
    ///     Throws when the effective runtime configuration violates the runtime trust policy.
    /// </summary>
    /// <param name="configuration">The composed application configuration.</param>
    /// <param name="environmentName">The effective host environment name.</param>
    internal static void ThrowIfUnsafeConfigurationExists(
        IConfiguration configuration,
        string environmentName
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);
        MississippiRuntimeTrustPolicy policy = MississippiRuntimeTrustPolicy.From(configuration, environmentName);
        foreach (KeyValuePair<string, string> value in EnumerateCandidateValues(configuration))
        {
            ValidateConfigurationValue(value.Key, value.Value, policy);
        }
    }

    private static MississippiRuntimeEndpointHostClass ClassifyHost(
        string host
    )
    {
        if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
        {
            return MississippiRuntimeEndpointHostClass.Local;
        }

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "host.docker.internal", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            return MississippiRuntimeEndpointHostClass.Local;
        }

        if (IPAddress.TryParse(host, out IPAddress? address) && IPAddress.IsLoopback(address))
        {
            return MississippiRuntimeEndpointHostClass.Local;
        }

        return MississippiRuntimeEndpointHostClass.External;
    }

    private static IEnumerable<KeyValuePair<string, string>> EnumerateCandidateValues(
        IConfiguration configuration
    )
    {
        HashSet<string> seenKeys = new(StringComparer.OrdinalIgnoreCase);
        foreach (IConfigurationSection child in configuration.GetSection("ConnectionStrings").GetChildren())
        {
            if (string.IsNullOrWhiteSpace(child.Value))
            {
                continue;
            }

            string key = $"ConnectionStrings:{child.Key}";
            if (seenKeys.Add(key))
            {
                yield return new(key, child.Value);
            }
        }

        foreach (KeyValuePair<string, string?> pair in configuration.AsEnumerable())
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
            {
                continue;
            }

            if (!LooksLikeEndpointSetting(pair.Key) || !seenKeys.Add(pair.Key))
            {
                continue;
            }

            yield return new(pair.Key, pair.Value);
        }
    }

    private static bool IsHostAllowed(
        string host,
        IReadOnlyCollection<string> allowedHosts
    ) =>
        allowedHosts.Any(allowedHost => string.Equals(host, allowedHost, StringComparison.OrdinalIgnoreCase) ||
                                        host.EndsWith($".{allowedHost}", StringComparison.OrdinalIgnoreCase));

    private static bool LooksLikeConnectionStringValue(
        string value
    ) =>
        value.Contains(';', StringComparison.Ordinal) ||
        value.Contains("AccountEndpoint=", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("UseDevelopmentStorage=", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("DefaultEndpointsProtocol=", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeEndpointSetting(
        string key
    ) =>
        EndpointValueKeySuffixes.Any(suffix => key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

    private static bool TryParseConnectionString(
        string value,
        out Dictionary<string, string> segments
    )
    {
        segments = new(StringComparer.OrdinalIgnoreCase);
        if (!LooksLikeConnectionStringValue(value))
        {
            return false;
        }

        string[] parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string part in parts)
        {
            int separatorIndex = part.IndexOf('=', StringComparison.Ordinal);
            if ((separatorIndex <= 0) || (separatorIndex == (part.Length - 1)))
            {
                continue;
            }

            string key = part[..separatorIndex].Trim();
            string segmentValue = part[(separatorIndex + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(key))
            {
                segments[key] = segmentValue;
            }
        }

        return segments.Count > 0;
    }

    private static void ValidateConfigurationValue(
        string sourceKey,
        string configuredValue,
        MississippiRuntimeTrustPolicy policy
    )
    {
        if (TryParseConnectionString(configuredValue, out Dictionary<string, string> segments))
        {
            ValidateConnectionString(sourceKey, segments, policy);
            return;
        }

        if (Uri.TryCreate(configuredValue, UriKind.Absolute, out Uri? endpointUri))
        {
            ValidateEndpoint(sourceKey, endpointUri, policy);
        }
    }

    private static void ValidateConnectionString(
        string sourceKey,
        Dictionary<string, string> segments,
        MississippiRuntimeTrustPolicy policy
    )
    {
        if (segments.TryGetValue("UseDevelopmentStorage", out string? useDevelopmentStorage) &&
            bool.TryParse(useDevelopmentStorage, out bool usesDevelopmentStorage) &&
            usesDevelopmentStorage)
        {
            if (!policy.AllowLocalEndpoints)
            {
                throw new InvalidOperationException(
                    $"Mississippi runtime [config-trust] rejects source '{sourceKey}': emulator defaults are only allowed in Development. Configure trusted external infrastructure or explicitly allow local endpoints for this environment.");
            }

            if (ConnectionStringEndpointKeys.Any(segments.ContainsKey))
            {
                throw new InvalidOperationException(
                    $"Mississippi runtime [config-trust] rejects source '{sourceKey}': emulator defaults cannot be combined with explicit endpoint overrides. Remove the conflicting override and keep one trusted endpoint path.");
            }

            return;
        }

        List<Uri> endpoints = [];
        foreach (string endpointKey in ConnectionStringEndpointKeys)
        {
            if (!segments.TryGetValue(endpointKey, out string? endpointValue))
            {
                continue;
            }

            if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out Uri? endpointUri))
            {
                throw new InvalidOperationException(
                    $"Mississippi runtime [config-trust] rejects source '{sourceKey}': endpoint token '{endpointKey}' is not a valid absolute URI.");
            }

            endpoints.Add(endpointUri);
        }

        if (endpoints.Count == 0)
        {
            ValidateDerivedConnectionStringTransport(sourceKey, segments, policy);
            return;
        }

        bool hasExternalEndpoint = endpoints.Any(endpoint =>
            ClassifyHost(endpoint.Host) == MississippiRuntimeEndpointHostClass.External);
        bool hasLocalEndpoint = endpoints.Any(endpoint =>
            ClassifyHost(endpoint.Host) != MississippiRuntimeEndpointHostClass.External);
        if (hasExternalEndpoint && hasLocalEndpoint)
        {
            throw new InvalidOperationException(
                $"Mississippi runtime [config-trust] rejects source '{sourceKey}': the endpoint set mixes local or emulator hosts with external hosts. Use one trusted endpoint class per configuration source.");
        }

        bool hasSecureExternalEndpoint = endpoints.Any(endpoint =>
            (ClassifyHost(endpoint.Host) == MississippiRuntimeEndpointHostClass.External) &&
            string.Equals(endpoint.Scheme, "https", StringComparison.OrdinalIgnoreCase));
        bool hasInsecureExternalEndpoint = endpoints.Any(endpoint =>
            (ClassifyHost(endpoint.Host) == MississippiRuntimeEndpointHostClass.External) &&
            !string.Equals(endpoint.Scheme, "https", StringComparison.OrdinalIgnoreCase));
        if (hasSecureExternalEndpoint && hasInsecureExternalEndpoint)
        {
            throw new InvalidOperationException(
                $"Mississippi runtime [config-trust] rejects source '{sourceKey}': the endpoint set mixes secure and insecure external transport. Keep one trusted transport policy per configuration source.");
        }

        foreach (Uri endpoint in endpoints)
        {
            ValidateEndpoint(sourceKey, endpoint, policy);
        }
    }

    private static void ValidateDerivedConnectionStringTransport(
        string sourceKey,
        Dictionary<string, string> segments,
        MississippiRuntimeTrustPolicy policy
    )
    {
        if (!segments.TryGetValue("DefaultEndpointsProtocol", out string? defaultProtocol) ||
            string.IsNullOrWhiteSpace(defaultProtocol))
        {
            return;
        }

        if (!segments.TryGetValue("AccountName", out string? accountName) || string.IsNullOrWhiteSpace(accountName))
        {
            return;
        }

        string endpointSuffix = segments.TryGetValue("EndpointSuffix", out string? configuredSuffix) &&
                                !string.IsNullOrWhiteSpace(configuredSuffix)
            ? configuredSuffix
            : "core.windows.net";
        foreach (string endpointPrefix in StorageDefaultEndpointPrefixes)
        {
            Uri derivedEndpoint = new(
                $"{defaultProtocol}://{accountName}.{endpointPrefix}.{endpointSuffix}",
                UriKind.Absolute);
            ValidateEndpoint(sourceKey, derivedEndpoint, policy);
        }
    }

    private static void ValidateEndpoint(
        string sourceKey,
        Uri endpointUri,
        MississippiRuntimeTrustPolicy policy
    )
    {
        MississippiRuntimeEndpointHostClass hostClass = ClassifyHost(endpointUri.Host);
        if (hostClass != MississippiRuntimeEndpointHostClass.External)
        {
            if (!policy.AllowLocalEndpoints)
            {
                throw new InvalidOperationException(
                    $"Mississippi runtime [config-trust] rejects source '{sourceKey}': a loopback or local endpoint classification is only allowed in Development. Configure a trusted external endpoint or explicitly allow local endpoints for this environment.");
            }

            return;
        }

        if (!policy.AllowedExternalSchemes.Contains(endpointUri.Scheme, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Mississippi runtime [config-trust] rejects source '{sourceKey}': scheme '{endpointUri.Scheme}' is not allowed for external endpoints. Configure a trusted scheme or explicitly approve it in Mississippi:Runtime:Trust:AllowedExternalSchemes.");
        }

        if ((policy.AllowedExternalHosts.Count > 0) && !IsHostAllowed(endpointUri.Host, policy.AllowedExternalHosts))
        {
            throw new InvalidOperationException(
                $"Mississippi runtime [config-trust] rejects source '{sourceKey}': the external endpoint host classification is not approved by Mississippi:Runtime:Trust:AllowedExternalHosts for this environment.");
        }
    }

    private sealed class MississippiRuntimeTrustPolicy
    {
        private MississippiRuntimeTrustPolicy(
            bool allowLocalEndpoints,
            HashSet<string> allowedExternalSchemes,
            HashSet<string> allowedExternalHosts
        )
        {
            AllowLocalEndpoints = allowLocalEndpoints;
            AllowedExternalSchemes = allowedExternalSchemes;
            AllowedExternalHosts = allowedExternalHosts;
        }

        internal bool AllowLocalEndpoints { get; }

        internal HashSet<string> AllowedExternalHosts { get; }

        internal HashSet<string> AllowedExternalSchemes { get; }

        internal static MississippiRuntimeTrustPolicy From(
            IConfiguration configuration,
            string environmentName
        )
        {
            IConfigurationSection section = configuration.GetSection("Mississippi:Runtime:Trust");
            bool isDevelopment = string.Equals(
                environmentName,
                Environments.Development,
                StringComparison.OrdinalIgnoreCase);
            bool allowLocalOutsideDevelopment = section.GetValue<bool>("AllowLocalEndpointsOutsideDevelopment");
            string[] schemes = ReadArray(section.GetSection("AllowedExternalSchemes"));
            if (schemes.Length == 0)
            {
                schemes = DefaultAllowedExternalSchemes;
            }

            return new(
                isDevelopment || allowLocalOutsideDevelopment,
                new(schemes, StringComparer.OrdinalIgnoreCase),
                new(ReadArray(section.GetSection("AllowedExternalHosts")), StringComparer.OrdinalIgnoreCase));
        }

        private static string[] ReadArray(
            IConfigurationSection section
        ) =>
            section.GetChildren()
                .Select(child => child.Value)
                .OfType<string>()
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
    }
}