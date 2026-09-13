---
id: brooks-storage-cosmos
title: Brooks Cosmos DB Provider
sidebar_label: Cosmos DB
sidebar_position: 2
description: Reference runtime composition, options, and storage ownership for the Brooks Cosmos DB provider.
---

# Brooks Cosmos DB Provider

The Brooks Cosmos DB provider stores event streams in Azure Cosmos DB and uses Azure Blob Storage for distributed
locking. Register it through the runtime composition builder so its options and service graph are staged with the
Orleans host.

## Applies to

- `Mississippi.Brooks.Runtime.Storage.Cosmos`
- `Mississippi.Brooks.Runtime.Storage.Abstractions`
- Orleans runtime hosts using `Mississippi.Hosting.Runtime` or `Mississippi.Sdk.Runtime`

This page covers Brooks event storage. Snapshot storage has its own provider and configuration surface.

## Contract

The provider entry point is `AddCosmosBrookStorageProvider` on `IRuntimeBuilder`:

```csharp
runtime.AddCosmosBrookStorageProvider(cosmos =>
{
    cosmos.CosmosClientServiceKey = "spring-cosmos";
    cosmos.DatabaseId = "spring-db";
    cosmos.ContainerId = "events";
});
```

Call it inside the single `siloBuilder.UseMississippi(...)` callback. The callback receives a
`CosmosBrookStorageBuilder` whose nine properties are copied into one final options snapshot before the provider
graph is staged.

The package also provides connection-string-owned overloads:

```csharp
runtime.AddCosmosBrookStorageProvider(
    cosmosConnectionString,
    blobStorageConnectionString,
    cosmos => cosmos.DatabaseId = "spring-db");
```

Configuration overloads bind the same `BrookStorageOptions` property names. All overloads target the runtime builder;
the provider-specific `IServiceCollection` registration surface is not the composition path.

## Client ownership

Choose one ownership mode for a host:

| Mode | Host responsibility | Provider responsibility |
| --- | --- | --- |
| Host-owned | Expose a keyed `CosmosClient` under the final `CosmosClientServiceKey` and a keyed `BlobServiceClient` under `BrookCosmosDefaults.BlobLockingServiceKey`; either may be forwarded from another host registration. | Stage the Brooks services and resolve those keyed clients when the graph is used. |
| Connection-string-owned | Supply both connection strings to the runtime overload. | Lazily register keyed SDK client factories using the final Cosmos key and the fixed Brooks Blob key. |

Host-owned mode creates no SDK clients and does not contact Cosmos or Blob during composition. Neither mode adds an
unkeyed client for Brooks. A selected custom Cosmos key therefore must be used consistently by the host registration
and the nested builder.

Spring uses host-owned mode. Aspire supplies the account clients, and the host forwards the Cosmos client to its
`spring-cosmos` key and the Blob client to `BrookCosmosDefaults.BlobLockingServiceKey` before `UseMississippi(...)`
attaches the runtime graph.

## Options

`CosmosBrookStorageBuilder` exposes the following `BrookStorageOptions` values.

| Property | Default | Meaning |
| --- | ---: | --- |
| `ContainerId` | `brooks` | Cosmos container for event and cursor documents. |
| `CosmosClientServiceKey` | `mississippi-cosmos-brooks-client` | Key used to resolve the `CosmosClient`. |
| `DatabaseId` | `mississippi` | Cosmos database identifier. |
| `LeaseDurationSeconds` | `60` | Finite Blob lease duration used by distributed locks. |
| `LeaseRenewalThresholdSeconds` | `20` | Renewal threshold used by the lock implementation. |
| `LockContainerName` | `locks` | Blob container used for lock blobs. |
| `MaxEventsPerBatch` | `90` | Maximum number of events placed in one write batch. |
| `MaxRequestSizeBytes` | `1_700_000` | Maximum estimated request size used when splitting write batches. |
| `QueryBatchSize` | `100` | Maximum items requested in one Cosmos query page. |

The keyed provider container is registered under `BrookCosmosDefaults.CosmosContainerServiceKey`,
`mississippi-cosmos-brooks`. That key is an internal provider-graph identity; it is separate from the selected
Cosmos client key.

## Constraints

- `ContainerId`, `CosmosClientServiceKey`, `DatabaseId`, and `LockContainerName` must be nonempty and non-whitespace.
- `MaxEventsPerBatch` must be positive.
- `MaxRequestSizeBytes` must be greater than the local batch envelope of `8_192` bytes. This is an estimator lower
  bound, not a claim about a Cosmos service limit.
- `QueryBatchSize` must be positive or `-1`. `-1` selects the Cosmos SDK's dynamic query page size; zero and values
  below `-1` are rejected. The value limits each page request, not the total number of matching events.
- `LeaseDurationSeconds` must be finite and between `15` and `60` seconds.
- `LeaseRenewalThresholdSeconds` must satisfy `0 <= threshold < LeaseDurationSeconds`.

The scalar options are validated when the nested composition is applied and again through the provider's startup
options validation. Required host-owned keyed clients are checked against the completed dependency-injection graph
when the options are resolved or startup validation runs; a later staged native callback may supply those clients.
A captured nested builder cannot change the final snapshot after its callback closes.

## Behavior

The runtime extension stages the provider's reader, writer, recovery, mapping, locking, and Cosmos repository
services. The provider exposes the Brooks contracts `IBrookStorageProvider`, `IBrookStorageReader`, and
`IBrookStorageWriter`; the concrete provider reports the format identifier `cosmos-db`.

The asynchronous container initializer runs as an `IHostedService` when the host starts. It creates the configured
database and container when they do not exist. An existing container must use the `/brookPartitionKey` partition-key
path. A different path fails startup and is not deleted. Composition itself only stages descriptors; it does not
build a service provider, open a network connection, or initialize Cosmos resources.

Writes use the configured event-count and estimated-size limits. Reads pass `QueryBatchSize` to the Cosmos query
page request and continue draining the async iterator. Blob leases coordinate distributed writes. The read, write, and
startup initialization operations accept cancellation tokens where their contracts expose them.

The Cosmos database identifier, event-container identifier, and `/brookPartitionKey` path define the persisted event
and cursor storage identity. `LockContainerName` identifies the Blob container used for lock coordination. The
Cosmos client key, fixed Blob client key, and keyed provider-container key are dependency-injection lookup aliases;
changing an alias does not move data when it still resolves to the same account and containers. Preserve the persisted
storage identities when an existing deployment must continue reading its data.

## Errors and diagnostics

Scalar option failures, a closed nested scope, and duplicate composition are reported by
`BuilderValidationException`. Its `Diagnostics` collection contains a stable `Code`, a `Message`, and `Remediation`
guidance, so callers can handle those failures without parsing message text. The provider's startup options validator
checks the completed dependency-injection graph for required host-owned keyed clients and final options. Those failures
are reported through `OptionsValidationException` when the options are resolved or startup validation runs, rather
than being required to appear before every staged callback. Null arguments, blank connection strings, and binding
errors remain ordinary argument or configuration exceptions.

The named constants in `BrookStorageBuilderDiagnosticCodes` are the stable part of the diagnostic contract:

| Code | Failure |
| --- | --- |
| `MSB301` | `ContainerId` is empty or whitespace. |
| `MSB302` | `CosmosClientServiceKey` is empty or whitespace. |
| `MSB303` | `DatabaseId` is empty or whitespace. |
| `MSB304` | `LockContainerName` is empty or whitespace. |
| `MSB305` | `QueryBatchSize` is zero or below `-1`. |
| `MSB306` | `LeaseDurationSeconds` is outside the finite `15..60` range. |
| `MSB307` | `LeaseRenewalThresholdSeconds` is negative or not less than the lease duration. |
| `MSB308` | `MaxEventsPerBatch` is not positive. |
| `MSB309` | `MaxRequestSizeBytes` does not exceed the local batch envelope. |
| `MSB310` | The selected keyed `CosmosClient` is not registered. |
| `MSB311` | The fixed keyed `BlobServiceClient` is not registered. |
| `MSB312` | The nested configuration scope is closed. |
| `MSB313` | Brooks Cosmos storage is composed more than once for the runtime. |

Message wording and remediation text describe the particular property or registration and may provide more context.

## Compatibility

The runtime-builder entry point replaces the old provider-specific `IServiceCollection` registration calls:

| Previous shape | Current shape |
| --- | --- |
| `services.AddCosmosBrookStorageProvider(...)` | `runtime.AddCosmosBrookStorageProvider(...)` inside `UseMississippi(...)` |
| `services.RegisterBrookStorageProvider<TProvider>()` for a custom provider | Advanced `runtime.ConfigureSilo(staged => ...)` service registration |

The nested builder starts with its own defaults and does not import values from an earlier
`services.Configure<BrookStorageOptions>(...)` registration. Move those values into the nested callback or the
supplied `IConfiguration` section. Standard `PostConfigure<BrookStorageOptions>` processing still runs in its normal
later options pipeline.

The generic helper is no longer part of the storage-abstractions composition contract. A custom provider must register
the Brooks storage contracts explicitly through the staged runtime service collection; it is responsible for its own
repository, durability, concurrency, and lifecycle behavior. Snapshot Cosmos composition is separate and is not changed
by this provider entry point.

## Example

This fragment is the host-owned pattern used by Spring. The host registrations for the `CosmosClient` and
`BlobServiceClient` are shown as forwarding operations; the actual account clients are supplied by Aspire before this
runtime callback runs.

```csharp
const string sharedCosmosKey = "spring-cosmos";

builder.Services.AddKeyedSingleton(
    BrookCosmosDefaults.BlobLockingServiceKey,
    (sp, _) => sp.GetRequiredKeyedService<BlobServiceClient>("blobs"));
builder.Services.AddKeyedSingleton(
    sharedCosmosKey,
    (sp, _) => sp.GetRequiredService<CosmosClient>());

builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseMississippi(runtime =>
    {
        runtime.AddCosmosBrookStorageProvider(cosmos =>
        {
            cosmos.CosmosClientServiceKey = sharedCosmosKey;
            cosmos.DatabaseId = "spring-db";
            cosmos.ContainerId = "events";
            cosmos.QueryBatchSize = 50;
            cosmos.MaxEventsPerBatch = 50;
        });
        runtime.AddEventSourcing();
        runtime.ApplyToSilo(siloBuilder);
    });
});
```

The source clients must already exist in the host graph: Spring resolves `CosmosClient` from its unkeyed Aspire
registration and `BlobServiceClient` from the keyed `blobs` registration. The forwarding registrations expose the
keyed targets required by Brooks; the provider callback only selects those targets and stages the Brooks graph.

## Summary

Use `AddCosmosBrookStorageProvider` on the runtime builder, select one client-ownership mode, validate the nine
options, and preserve the database, container, lock, client-key, and partition-key identities required by the
deployment.

## Next steps

- [Storage provider concepts](index.md)
- [Runtime composition](../../reference/runtime-composition.md)
- [Brooks operations](../operations/operations.md)
- [Brooks troubleshooting](../troubleshooting/troubleshooting.md)
