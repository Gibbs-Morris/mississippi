using System;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Runtime;
using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Brooks.Runtime;
using Mississippi.Brooks.Runtime.Factory;
using Mississippi.Brooks.Runtime.Reader;
using Mississippi.Brooks.Runtime.Storage.Cosmos;
using Mississippi.Brooks.Serialization.Json;
using Mississippi.DomainModeling.Abstractions.Builders;
using Mississippi.DomainModeling.Runtime.Builders;
using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Generators.Abstractions;
using Mississippi.Inlet.Runtime;
using Mississippi.Inlet.Runtime.Abstractions;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime;
using Mississippi.Tributary.Runtime.Storage.Cosmos;

using Orleans.Hosting;


namespace Mississippi.Sdk.Runtime;

/// <summary>
///     Root builder for Mississippi runtime-side composition, attached from
///     <see cref="SiloBuilderExtensions.UseMississippi" /> inside <c>UseOrleans(...)</c>.
/// </summary>
/// <remarks>
///     <para>
///         This builder owns runtime subsystem composition: aggregates, projections, sagas,
///         event sourcing, snapshots, serialization, and runtime-side integrations.
///     </para>
///     <para>
///         Domain scopes are accessed through sub-builder callbacks:
///         <c>runtime.Aggregates(...)</c>, <c>runtime.Projections(...)</c>, <c>runtime.Sagas(...)</c>.
///     </para>
///     <para>
///         Infrastructure methods such as <c>AddJsonSerialization()</c> and
///         <c>AddCosmosEventStorage(...)</c> stay flat on the runtime builder.
///     </para>
/// </remarks>
public sealed class MississippiRuntimeBuilder
{
    private readonly IServiceCollection services;

    private readonly ISiloBuilder siloBuilder;

    private AggregateBuilder? aggregateBuilder;

    private bool aqueductAdded;

    private bool cosmosEventStorageAdded;

    private bool cosmosSnapshotStorageAdded;

    private bool eventSourcingAdded;

    private bool inletRuntimeAdded;

    private bool jsonSerializationAdded;

    private ProjectionAuthorizationRegistry? projectionAuthorizationRegistry;

    private ProjectionBrookRegistry? projectionBrookRegistry;

    private ProjectionBuilder? projectionBuilder;

    private SagaBuilder? sagaBuilder;

    private bool snapshotCachingAdded;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MississippiRuntimeBuilder" /> class.
    /// </summary>
    /// <param name="siloBuilder">The Orleans silo builder.</param>
    internal MississippiRuntimeBuilder(
        ISiloBuilder siloBuilder
    )
    {
        ArgumentNullException.ThrowIfNull(siloBuilder);
        this.siloBuilder = siloBuilder;
        services = siloBuilder.Services;
    }

    /// <summary>
    ///     Configures Aqueduct for SignalR backplane support on the silo.
    ///     Idempotent — safe to call multiple times.
    /// </summary>
    /// <param name="configure">Action to configure Aqueduct silo options.</param>
    public void AddAqueduct(
        Action<AqueductSiloOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (aqueductAdded)
        {
            return;
        }

        aqueductAdded = true;
        AqueductSiloOptions options = new(siloBuilder);
        configure(options);
        services.Configure<AqueductOptions>(aqueductOptions =>
        {
            aqueductOptions.StreamProviderName = options.StreamProviderName;
            aqueductOptions.ServerStreamNamespace = options.ServerStreamNamespace;
            aqueductOptions.AllClientsStreamNamespace = options.AllClientsStreamNamespace;
            aqueductOptions.HeartbeatIntervalMinutes = options.HeartbeatIntervalMinutes;
            aqueductOptions.DeadServerTimeoutMultiplier = options.DeadServerTimeoutMultiplier;
        });
        services.TryAddSingleton<IAqueductGrainFactory, AqueductGrainFactory>();
    }

    /// <summary>
    ///     Configures Cosmos DB storage for the append-only event stream (Brooks layer).
    ///     Idempotent — safe to call multiple times.
    /// </summary>
    /// <param name="configure">Action to configure Cosmos event storage options.</param>
    public void AddCosmosEventStorage(
        Action<BrookStorageOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (cosmosEventStorageAdded)
        {
            return;
        }

        cosmosEventStorageAdded = true;
        services.Configure(configure);
        BrookCosmosStorageRegistration.RegisterServices(services);
    }

    /// <summary>
    ///     Configures Cosmos DB storage for aggregate and projection snapshots.
    ///     Idempotent — safe to call multiple times.
    /// </summary>
    /// <param name="configure">Action to configure Cosmos snapshot storage options.</param>
    public void AddCosmosSnapshotStorage(
        Action<SnapshotStorageOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (cosmosSnapshotStorageAdded)
        {
            return;
        }

        cosmosSnapshotStorageAdded = true;
        services.Configure(configure);
        SnapshotCosmosStorageRegistration.RegisterServices(services);
    }

    /// <summary>
    ///     Configures event sourcing with the specified stream provider options.
    ///     Idempotent — safe to call multiple times.
    /// </summary>
    /// <param name="configure">Action to configure event sourcing options.</param>
    public void AddEventSourcing(
        Action<BrookProviderOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (eventSourcingAdded)
        {
            return;
        }

        eventSourcingAdded = true;
        services.Configure(configure);

        // Register Brooks runtime services
        services.AddSingleton<BrookGrainFactory>();
        services.AddSingleton<IBrookGrainFactory>(sp => sp.GetRequiredService<BrookGrainFactory>());
        services.AddSingleton<IInternalBrookGrainFactory>(sp => sp.GetRequiredService<BrookGrainFactory>());
        services.AddSingleton<IStreamIdFactory, StreamIdFactory>();
        services.AddOptions<BrookReaderOptions>();
        services.AddOptions<BrookProviderOptions>();
    }

    /// <summary>
    ///     Adds Inlet runtime services for projection subscription management.
    ///     Idempotent — safe to call multiple times.
    /// </summary>
    public void AddInletRuntime()
    {
        if (inletRuntimeAdded)
        {
            return;
        }

        inletRuntimeAdded = true;
        EnsureProjectionRegistries();
    }

    /// <summary>
    ///     Registers the JSON serialization provider for event and snapshot payloads.
    ///     Idempotent — safe to call multiple times.
    /// </summary>
    public void AddJsonSerialization()
    {
        if (jsonSerializationAdded)
        {
            return;
        }

        jsonSerializationAdded = true;
        JsonSerializationRegistrations.AddJsonSerialization(services);
    }

    /// <summary>
    ///     Adds snapshot caching infrastructure services.
    ///     Idempotent — safe to call multiple times.
    /// </summary>
    public void AddSnapshotCaching()
    {
        if (snapshotCachingAdded)
        {
            return;
        }

        snapshotCachingAdded = true;
        services.TryAddSingleton<ISnapshotGrainFactory, SnapshotGrainFactory>();
    }

    /// <summary>
    ///     Configures aggregate registrations through a sub-builder callback.
    /// </summary>
    /// <param name="configure">The aggregate configuration callback.</param>
    public void Aggregates(
        Action<IAggregateBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        aggregateBuilder ??= new(services);
        configure(aggregateBuilder);
    }

    /// <summary>
    ///     Configures projection registrations through a sub-builder callback.
    /// </summary>
    /// <param name="configure">The projection configuration callback.</param>
    public void Projections(
        Action<IProjectionBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        projectionBuilder ??= new(services);
        configure(projectionBuilder);
    }

    /// <summary>
    ///     Registers explicit projection metadata for runtime subscription and authorization handling.
    /// </summary>
    /// <typeparam name="TProjection">The projection type carrying the metadata attributes.</typeparam>
    public void RegisterProjectionMetadata<TProjection>()
        where TProjection : class
    {
        AddInletRuntime();
        Type projectionType = typeof(TProjection);
        ProjectionPathAttribute pathAttribute = projectionType.GetCustomAttribute<ProjectionPathAttribute>() ??
                                                throw new InvalidOperationException(
                                                    $"Projection type '{projectionType.FullName}' must declare {nameof(ProjectionPathAttribute)} to be registered.");
        BrookNameAttribute? brookNameAttribute = projectionType.GetCustomAttribute<BrookNameAttribute>();
        string brookName = brookNameAttribute?.BrookName ?? pathAttribute.Path;
        projectionBrookRegistry!.Register(pathAttribute.Path, brookName);
        GenerateAuthorizationAttribute? authorizationAttribute =
            projectionType.GetCustomAttribute<GenerateAuthorizationAttribute>();
        bool hasAuthorize = authorizationAttribute is not null;
        bool hasAllowAnonymous = projectionType.GetCustomAttribute<GenerateAllowAnonymousAttribute>() is not null;
        if (!hasAuthorize && !hasAllowAnonymous)
        {
            return;
        }

        projectionAuthorizationRegistry!.Register(
            pathAttribute.Path,
            new(
                authorizationAttribute?.Policy,
                authorizationAttribute?.Roles,
                authorizationAttribute?.AuthenticationSchemes,
                hasAuthorize,
                hasAllowAnonymous));
    }

    /// <summary>
    ///     Configures saga registrations through a sub-builder callback.
    /// </summary>
    /// <param name="configure">The saga configuration callback.</param>
    public void Sagas(
        Action<ISagaBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        sagaBuilder ??= new(services);
        configure(sagaBuilder);
    }

    /// <summary>
    ///     Validates all sub-builder state for correctness.
    /// </summary>
    internal void Validate()
    {
        aggregateBuilder?.Validate();
        projectionBuilder?.Validate();
        sagaBuilder?.Validate();
    }

    private void EnsureProjectionRegistries()
    {
        projectionBrookRegistry ??= new();
        projectionAuthorizationRegistry ??= new();
        services.RemoveAll<IProjectionBrookRegistry>();
        services.AddSingleton<IProjectionBrookRegistry>(projectionBrookRegistry);
        services.RemoveAll<IProjectionAuthorizationRegistry>();
        services.AddSingleton<IProjectionAuthorizationRegistry>(projectionAuthorizationRegistry);
    }
}