---
id: tributary-storage-providers
title: Tributary Storage Providers
sidebar_label: Overview
sidebar_position: 1
description: Understand Tributary snapshot storage ownership, runtime composition, and provider boundaries.
---

# Tributary Storage Providers

Tributary storage providers turn the snapshot storage contracts into a durable backend while the runtime host owns
composition and host-managed dependencies.

## The problem this solves

Snapshot grains need a reader and writer without coupling Tributary to one database client, resource identity, or
deployment model. A provider isolates persistence and mapping details behind `ISnapshotStorageProvider`, while a host
can keep control of clients and configuration.

## Core idea

The storage abstraction has three related contracts:

- `ISnapshotStorageProvider` combines snapshot reads, writes, deletes, and pruning and exposes a `Format` identifier.
- `ISnapshotStorageReader` supplies the read side used by snapshot cache grains.
- `ISnapshotStorageWriter` supplies writes, deletes, and pruning used by snapshot persistence.

Provider packages compose their graph through the shared `IRuntimeBuilder` scope. The Cosmos implementation uses
`runtime.AddCosmosSnapshotStorageProvider(...)` inside `UseMississippi(...)`; it does not add a second runtime root or
expose a provider-specific nested service collection.

## How it works

1. The host registers or provisions the backend clients it owns. Host-owned Cosmos composition expects a keyed
   `CosmosClient` under the selected client service key.
2. Runtime composition stages the provider options and service descriptors with the Orleans silo. The nested Snapshot
   builder starts with its defaults and captures the four configured values in one final options snapshot.
3. Final options validation checks the completed dependency-injection graph for the selected keyed client. The provider
   does not construct that client or contact Cosmos during composition.
4. At host startup, the Cosmos hosted initializer creates the configured database and snapshot container when needed,
   and checks the `/snapshotPartitionKey` partition-key path.
5. Tributary grains call the reader and writer contracts. The Cosmos provider maps snapshot envelopes and keys to
   documents; exact options and mapping rules are in the [Cosmos reference](cosmos.md).

The provider also supports connection-string-owned composition. That overload registers a lazy keyed client factory under
the final selected key. Host-owned and connection-string-owned modes differ in client ownership; they share the same
snapshot contracts and provider graph.

The default provider is registered with `TryAddSingleton`. The reader and writer registrations resolve that provider,
so the default provider and a pre-registered singleton custom provider share one instance across all three contracts. A
pre-registered custom provider with another lifetime remains under its existing descriptor; the Cosmos composition does
not normalize that lifetime.

For a custom snapshot provider, use the advanced staged native hook and register the contracts explicitly. This is a
registration shape; `MySnapshotStorageProvider` represents the application's implementation:

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

The custom implementation owns its repository, storage backend, lifecycle, and durability behavior. `ConfigureSilo(...)`
supplies the staged `ISiloBuilder.Services` surface; it does not infer those behaviors or create a generic provider
framework.

## Guarantees

- Runtime composition keeps provider descriptors in the staged runtime graph until terminal attachment. The [runtime
  composition contract](../../reference/runtime-composition.md) defines validation, publication, and restoration when
  attachment succeeds or fails.
- The Cosmos provider preserves the Tributary reader, writer, mapper, repository, and hosted-initializer roles.
- The Cosmos provider preserves snapshot resource and key identities, including the configured database and container,
  `/snapshotPartitionKey` path, stable snapshot storage names, stream keys, snapshot keys, and reducer hashes when
  callers keep those values unchanged. See the [Cosmos reference](cosmos.md) for the exact mapping.
- Host-owned composition does not construct or replace the host's keyed client during staging. Connection-string-owned
  composition may add its own keyed factory under its selected key.
- A default or pre-registered singleton provider is the same instance when resolved through
  `ISnapshotStorageProvider`, `ISnapshotStorageReader`, and `ISnapshotStorageWriter`.

## Non-guarantees

- A DI service key is a lookup alias. Renaming it does not migrate a Cosmos database, container, partition key, or
  snapshot document when the alias still resolves to the same resources.
- Composition and options resolution validate option values and keyed registration availability; they do not prove
  credentials, network connectivity, database access, or partition-key compatibility. Cosmos access and partition-key
  checks occur in the hosted initializer at startup.
- Composition does not normalize a custom provider's non-singleton lifetime or supply its persistence, retry, or recovery
  behavior.
- The storage abstraction does not make Cosmos equivalent to another backend. Provider-specific query, throughput,
  failure, and durability behavior remains outside these shared contracts.
- Staged runtime publication has failure and restoration boundaries described by the runtime composition page; callers
  should use that contract instead of assuming every host publication failure is recoverable in place.

## Trade-offs

Runtime composition makes backend ownership explicit and lets Aspire or another host share clients across providers. It
also requires the host registration and the nested builder to agree on the keyed client name. Connection-string-owned mode
reduces host setup for a focused runtime, but leaves client credentials and lifetime with the provider registration.

The shared provider/reader/writer contracts keep Tributary independent of Cosmos. A custom provider gets the same
abstraction boundary, but must implement and validate its own storage semantics through the advanced hook.

## Related tasks and reference

- [Cosmos provider reference](cosmos.md) for overloads, options, defaults, constraints, diagnostics, and document mapping.
- [Runtime composition](../../reference/runtime-composition.md) for staged services, native Orleans configuration, and
  terminal attachment.
- [Tributary operations](../operations/operations.md) for the operational boundary.
- [Tributary overview](../index.md) for the reducer and snapshot layer.
- [Brooks storage providers](../../brooks/storage-providers/index.md) for the event-stream persistence layer below
  Tributary.
