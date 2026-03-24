using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Hosting.Runtime;

/// <summary>
///     Validates runtime-owned host composition before Orleans startup is finalized.
/// </summary>
internal static class MississippiRuntimeCompositionGuards
{
    /// <summary>
    ///     Throws when unsupported same-host or competing Orleans ownership markers are present.
    /// </summary>
    /// <param name="services">The service collection to inspect.</param>
    internal static void ThrowIfUnsupportedCompositionExists(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.Any(descriptor => descriptor.ServiceType == typeof(MississippiGatewayHostModeMarker)))
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
}
