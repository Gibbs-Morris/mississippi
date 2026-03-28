using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Orleans.Hosting;


namespace Mississippi.Hosting.Runtime;

/// <summary>
///     Stores deferred runtime-builder configuration until Orleans attaches to the host.
/// </summary>
internal sealed class MississippiRuntimeBuilderState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiRuntimeBuilderState" /> class.
    /// </summary>
    /// <param name="hostServices">The host service collection that owns runtime composition.</param>
    /// <param name="configuration">The effective host configuration.</param>
    /// <param name="hostEnvironment">The effective host environment.</param>
    internal MississippiRuntimeBuilderState(
        IServiceCollection hostServices,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment
    )
    {
        ArgumentNullException.ThrowIfNull(hostServices);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        Configuration = configuration;
        HostEnvironment = hostEnvironment;
        HostServices = hostServices;
    }

    /// <summary>
    ///     Gets the queued Orleans configuration callbacks for verification scenarios.
    /// </summary>
    internal IReadOnlyList<Action<ISiloBuilder>> QueuedOrleansConfigurations => OrleansConfigurations;

    private IConfiguration Configuration { get; }

    private IHostEnvironment HostEnvironment { get; }

    private IServiceCollection HostServices { get; }

    private IReadOnlyDictionary<string, int> FrozenHostOrleansOwnership { get; set; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    private bool HasFrozenHostOrleansOwnership { get; set; }

    private HashSet<string> RegisteredDomains { get; } = new(StringComparer.Ordinal);

    private List<Action<ISiloBuilder>> OrleansConfigurations { get; } = [];

    /// <summary>
    ///     Applies all queued Orleans configuration callbacks to the provided silo builder.
    /// </summary>
    /// <param name="siloBuilder">The silo builder receiving the queued configuration.</param>
    internal void ApplyOrleansConfiguration(
        ISiloBuilder siloBuilder
    )
    {
        ArgumentNullException.ThrowIfNull(siloBuilder);
        IReadOnlyDictionary<string, int> frozenOrleansOwnership =
            MississippiRuntimeCompositionGuards.CaptureFrozenOrleansOwnership(siloBuilder.Services);
        MississippiRuntimeConfigurationTrustGuards.ThrowIfUnsafeConfigurationExists(
            Configuration,
            HostEnvironment.EnvironmentName);
        MississippiRuntimeCompositionGuards.ThrowIfUnsupportedCompositionExists(HostServices);
        if (HasFrozenHostOrleansOwnership)
        {
            MississippiRuntimeCompositionGuards.ThrowIfFrozenOrleansOwnershipChanged(
                HostServices,
                FrozenHostOrleansOwnership);
        }

        MississippiRuntimeCompositionGuards.ThrowIfUnsupportedCompositionExists(siloBuilder.Services);
        foreach (Action<ISiloBuilder> configure in OrleansConfigurations)
        {
            configure(siloBuilder);
        }

        MississippiRuntimeConfigurationTrustGuards.ThrowIfUnsafeConfigurationExists(
            Configuration,
            HostEnvironment.EnvironmentName);
        MississippiRuntimeCompositionGuards.ThrowIfUnsupportedCompositionExists(HostServices);
        if (HasFrozenHostOrleansOwnership)
        {
            MississippiRuntimeCompositionGuards.ThrowIfFrozenOrleansOwnershipChanged(
                HostServices,
                FrozenHostOrleansOwnership);
        }

        MississippiRuntimeCompositionGuards.ThrowIfUnsupportedCompositionExists(siloBuilder.Services);
        MississippiRuntimeCompositionGuards.ThrowIfFrozenOrleansOwnershipChanged(
            siloBuilder.Services,
            frozenOrleansOwnership);
    }

    /// <summary>
    ///     Freezes the Mississippi-owned host Orleans attachment state after runtime registration.
    /// </summary>
    internal void FreezeHostOrleansOwnership()
    {
        FrozenHostOrleansOwnership = MississippiRuntimeCompositionGuards.CaptureFrozenOrleansOwnership(HostServices);
        HasFrozenHostOrleansOwnership = true;
    }

    /// <summary>
    ///     Throws when a generated runtime domain registration is attached more than once.
    /// </summary>
    /// <param name="domainName">The normalized domain name being attached.</param>
    /// <param name="registrationMethodName">The generated registration method name.</param>
    internal void EnsureDomainRegistrationAvailable(
        string domainName,
        string registrationMethodName
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainName);
        ArgumentException.ThrowIfNullOrWhiteSpace(registrationMethodName);
        if (RegisteredDomains.Add(domainName))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Mississippi runtime domain composition for '{domainName}' can only be attached once per builder. Remove the duplicate {registrationMethodName}(...) call and keep each domain on a single runtime builder path.");
    }

    /// <summary>
    ///     Queues an Orleans configuration callback for later application.
    /// </summary>
    /// <param name="configure">The callback to queue.</param>
    internal void QueueOrleansConfiguration(
        Action<ISiloBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        OrleansConfigurations.Add(configure);
    }
}