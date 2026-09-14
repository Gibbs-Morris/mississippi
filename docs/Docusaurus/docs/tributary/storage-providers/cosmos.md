---
id: tributary-storage-cosmos
title: Tributary Cosmos DB Provider
sidebar_label: Cosmos DB
sidebar_position: 2
description: Reference runtime composition, options, storage identity, and failure behavior for Tributary snapshot storage in Cosmos DB.
---

# Tributary Cosmos DB Provider

The Tributary Cosmos DB provider persists snapshot envelopes in Azure Cosmos DB. Compose it through `IRuntimeBuilder` so
the provider options and service graph are staged with the Orleans runtime.

## Applies to

- `Mississippi.Tributary.Runtime.Storage.Cosmos`
- `Mississippi.Tributary.Runtime.Storage.Abstractions`
- Orleans hosts using `Mississippi.Hosting.Runtime` or `Mississippi.Sdk.Runtime`

This page covers snapshot storage. Brooks event storage has a separate [Cosmos provider reference](../../brooks/storage-providers/cosmos.md).

## Contract

The host-owned entry point is `AddCosmosSnapshotStorageProvider` on `IRuntimeBuilder`:

```csharp
runtime.AddCosmosSnapshotStorageProvider(snapshot =>
{
    snapshot.CosmosClientServiceKey = "spring-cosmos";
    snapshot.DatabaseId = "spring-db";
    snapshot.ContainerId = "snapshots";
    snapshot.QueryBatchSize = 100;
});
```

Call it inside the `siloBuilder.UseMississippi(...)` callback. The callback receives a
`CosmosSnapshotStorageBuilder`. Its four properties are copied into one final options snapshot before the provider
graph is staged.

The overloads are:

| Signature | Client ownership | Configuration source |
| --- | --- | --- |
| `AddCosmosSnapshotStorageProvider(Action<CosmosSnapshotStorageBuilder>?)` | Host-owned keyed `CosmosClient` | Nested callback, optional |
| `AddCosmosSnapshotStorageProvider(string, Action<CosmosSnapshotStorageBuilder>?)` | Provider-owned connection string | Nested callback, optional |
| `AddCosmosSnapshotStorageProvider(IConfiguration)` | Host-owned keyed `CosmosClient` | Configuration property names |
| `AddCosmosSnapshotStorageProvider(string, IConfiguration)` | Provider-owned connection string | Configuration property names |

The configuration overloads bind `SnapshotStorageOptions` property names from the supplied configuration object or
section. A callback and a configuration object are not combined by one overload; use the callback overload when code
needs to override values explicitly.

## Client ownership

Choose one client-ownership mode for a host:

| Mode | Host responsibility | Provider responsibility |
| --- | --- | --- |
| Host-owned | Register a keyed `CosmosClient` under the final `CosmosClientServiceKey`. | Stage the snapshot graph, check keyed registration availability during final options validation, and resolve the client when the graph or initializer needs it. |
| Connection-string-owned | Supply a nonblank connection string. | Register a lazy keyed `CosmosClient` factory under the final selected key. |

Host-owned composition does not create an SDK client or contact Cosmos during composition. Connection-string-owned
composition also registers its client factory without constructing the client during composition. The selected key is
the key used by the provider's final graph; a callback can change it.

The provider's keyed `Container` is registered under `SnapshotCosmosDefaults.CosmosContainerServiceKey`, whose value
is `mississippi-cosmos-snapshots`. Client and container service keys are dependency-injection lookup aliases. They do
not name a Cosmos database or container and do not by themselves migrate persisted data.

## Options

`CosmosSnapshotStorageBuilder` exposes the following `SnapshotStorageOptions` values:

| Property | Default | Meaning |
| --- | --- | --- |
| `ContainerId` | `snapshots` | Cosmos container identifier for snapshot documents. |
| `CosmosClientServiceKey` | `mississippi-cosmos-snapshots-client` | Key used to resolve the `CosmosClient`. |
| `DatabaseId` | `mississippi` | Cosmos database identifier. |
| `QueryBatchSize` | `100` | Maximum number of items requested per query page. |

The nested builder starts with these defaults. Values are preserved as assigned; option identifiers are not trimmed or
normalized.

## Constraints

- `ContainerId`, `CosmosClientServiceKey`, and `DatabaseId` must be nonempty and non-whitespace.
- `QueryBatchSize` must be positive or `-1`. `-1` selects the Cosmos SDK's dynamic query page-size behavior. Zero and
  values below `-1` are rejected.
- A host-owned configuration must expose the selected keyed `CosmosClient` in the completed dependency-injection
  graph. A later staged `ConfigureSilo(...)` callback may provide it.
- Compose Snapshot Cosmos once for a runtime. Combine settings in one `AddCosmosSnapshotStorageProvider(...)` call.

Scalar options are validated when the nested scope is applied during runtime composition. The final options validator
checks the completed graph when options resolve or startup validation runs; it checks keyed registration availability
without constructing the client.

## Registration and lifecycle

The extension stages the snapshot container operations, Cosmos repository, retry policy, five mappers, keyed `Container`,
and hosted container initializer. The provider reports the `cosmos-db` format identifier. `ISnapshotStorageProvider` uses
`TryAddSingleton`, so an existing unkeyed provider descriptor is preserved. The reader and writer aliases are factories
registered with the effective lifetime of the last unkeyed `ISnapshotStorageProvider` descriptor:

| Effective provider lifetime | Reader/writer alias lifetime | Identity behavior |
| --- | --- | --- |
| Singleton | Singleton | Provider, reader, and writer resolve the same instance. |
| Scoped | Scoped | They share one provider instance within a scope; another scope gets another instance. |
| Transient | Transient | Each alias remains transient; no cross-contract identity is promised. |

Keyed `ISnapshotStorageProvider` descriptors are independent of this unkeyed lifetime selection. The default provider is
therefore a shared singleton, while a pre-registered custom provider keeps its effective lifetime and alias behavior.
The lifetime is selected when the Snapshot native callback executes. To override the provider registration, register the
unkeyed provider on host services before `UseMississippi(...)`, or queue its `ConfigureSilo(...)` callback before
`AddCosmosSnapshotStorageProvider(...)` in the same runtime composition. Replacing the provider afterward, including in
a later `ConfigureSilo(...)` callback, is unsupported because the reader and writer alias lifetimes have already been
selected. Use the complete [custom-provider shape](./index.md) when replacing Cosmos persistence entirely.

The `CosmosContainerInitializer` runs when the host starts. It creates the configured database and container if they do
not exist, and requires the container partition-key path `/snapshotPartitionKey`. An existing container with another
partition path fails startup with `InvalidOperationException`; it is not deleted. Initialization calls accept the host's
cancellation token. Composition itself does not build a service provider, open a network connection, or initialize a
Cosmos resource.

## Persisted identity

The provider preserves the Tributary snapshot contracts and document mapping:

- `SnapshotStreamKey` keeps its Orleans alias and serialized member IDs `[Id(0..3)]`; its composite form is
  `brookName|snapshotStorageName|entityId|reducersHash`.
- `SnapshotKey` keeps its Orleans alias and serialized member IDs `[Id(0..1)]`; its composite form is
  `brookName|entityId|version|snapshotStorageName|reducersHash`, and its version is nonnegative.
- `SnapshotStorageNameAttribute` supplies the stable snapshot name used by the type registry in the form
  `APP.MODULE.NAME.Vn`. Renaming a CLR type does not change that name when the attribute values stay the same.
- `SnapshotEnvelope` preserves its Orleans alias and serialized member IDs `[Id(0..3)]`. Cosmos mapping round-trips its
  payload, content type, payload-size, and reducer-hash fields. The document's `reducersHash` is taken from
  `SnapshotStreamKey`.
- The Cosmos document id is the invariant string form of the snapshot version. `snapshotPartitionKey` is the stream-key
  composite, `projectionType` is the stable snapshot storage name, `projectionId` is the entity id, and `reducersHash`
  is the stream's reducer hash.

Database and container identifiers are persisted-resource identities. Client and container service keys are DI aliases;
renaming an alias while forwarding it to the same Cosmos resources does not move or migrate data. Preserve the database,
container, partition-key, snapshot-name, and reducer-hash contracts when an existing deployment must continue reading
its snapshots.

## Errors and diagnostics

Invalid scalar values, duplicate composition, and configuration of a closed nested scope use `BuilderValidationException`.
Its `Diagnostics` collection contains a stable code plus message and remediation guidance. Missing final keyed clients
and other final options failures use `OptionsValidationException` when options resolve or startup validation runs. Null
arguments, blank connection strings, and configuration binding failures remain ordinary argument or configuration
exceptions; callback exceptions propagate unchanged.

The stable Snapshot Cosmos diagnostic constants are:

| Code | Failure |
| --- | --- |
| `MSB401` | `ContainerId` is empty or whitespace. |
| `MSB402` | `CosmosClientServiceKey` is empty or whitespace. |
| `MSB403` | `DatabaseId` is empty or whitespace. |
| `MSB404` | `QueryBatchSize` is zero or below `-1`. |
| `MSB405` | The selected keyed `CosmosClient` is missing from the completed graph. |
| `MSB406` | The nested Snapshot Cosmos configuration scope is closed. |
| `MSB407` | Snapshot Cosmos is composed more than once for the runtime. |

Code values are the stable part of the diagnostic contract. Message wording and remediation text provide context for
the particular option or registration.

## Compatibility

The runtime-builder entry point replaces the previous provider-specific `IServiceCollection` registration path. Custom
snapshot providers use the advanced staged hook and register all three storage contracts:

```csharp
runtime.ConfigureSilo(staged =>
{
    staged.Services.AddSingleton<ISnapshotStorageProvider, MySnapshotStorageProvider>();
    staged.Services.AddSingleton<ISnapshotStorageReader>(sp =>
        sp.GetRequiredService<ISnapshotStorageProvider>());
    staged.Services.AddSingleton<ISnapshotStorageWriter>(sp =>
        sp.GetRequiredService<ISnapshotStorageProvider>());
});
```

That hook is responsible for the custom provider's reader, writer, storage, lifetime, and initialization behavior. The
Cosmos extension does not expose a nested service collection.

If an existing host configured `SnapshotStorageOptions` with `services.Configure<SnapshotStorageOptions>(...)`, move
those values into the nested callback or supplied configuration section. The nested builder starts from its own defaults
and does not import earlier options registrations. Standard `PostConfigure<SnapshotStorageOptions>` processing still
runs in its normal later options pipeline.

## Example

This source-verified fragment is the host-owned Snapshot registration used by the Spring runtime. Spring forwards its
host-created Cosmos client under `sharedCosmosKey` before this runtime callback runs:

```csharp
runtime.AddCosmosSnapshotStorageProvider(snapshot =>
{
    snapshot.CosmosClientServiceKey = sharedCosmosKey;
    snapshot.DatabaseId = "spring-db";
    snapshot.ContainerId = "snapshots";
    snapshot.QueryBatchSize = 100;
});
```

For a connection-string-owned host, use the string overload and keep the same nested options shape:

```csharp
runtime.AddCosmosSnapshotStorageProvider(
    cosmosConnectionString,
    snapshot => snapshot.DatabaseId = "spring-db");
```

These calls stage the provider. Database and container creation happens through the hosted initializer when the Orleans
host starts.

## Summary

Use `AddCosmosSnapshotStorageProvider` on the runtime builder, select host-owned or connection-string-owned client
composition, and preserve the configured Cosmos resource and snapshot identity contracts.

## Next steps

- [Storage provider concepts](index.md)
- [Runtime composition](../../reference/runtime-composition.md)
- [Tributary operations](../operations/operations.md)
- [Tributary troubleshooting](../troubleshooting/troubleshooting.md)
