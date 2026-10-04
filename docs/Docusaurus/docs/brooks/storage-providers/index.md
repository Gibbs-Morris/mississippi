---
id: brooks-storage-providers
title: Brooks Storage Providers
sidebar_label: Overview
sidebar_position: 1
description: Understand Brooks storage-provider contracts, runtime composition, and provider ownership.
---

# Brooks Storage Providers

Brooks storage providers implement the event-stream persistence contracts while the runtime host owns composition,
external clients, and deployment-specific identities.

## The problem this solves

Event streams need durable reads and writes, cursor recovery, and concurrency control, but those responsibilities
should not make Brooks runtime code depend on one database or lock service. A provider isolates those backend details
behind the Brooks storage contracts.

## Core idea

The storage abstraction has three related contracts:

- `IBrookStorageProvider` combines read and write access and identifies the backend through `Format`.
- `IBrookStorageReader` reads cursor positions and event ranges.
- `IBrookStorageWriter` appends events with an optional expected version for optimistic concurrency.

Runtime hosts compose a concrete provider through the canonical `IRuntimeBuilder` scope. For Cosmos DB, the provider
entry point is `runtime.AddCosmosBrookStorageProvider(...)`. The nested callback captures provider options and stages
the provider graph with the rest of the Orleans host.

## How it works

The ownership boundary has four steps:

1. The host provisions or registers the backend clients required by its environment. Host-owned Cosmos composition
   uses keyed `CosmosClient` and `BlobServiceClient` registrations.
2. `UseMississippi(...)` creates a staged runtime service collection. `AddCosmosBrookStorageProvider(...)` adds the
   provider, reader, writer, recovery, mapping, locking, and repository services to that staged collection.
3. Terminal runtime attachment validates and publishes the staged descriptors. Its failure handling and service-graph
   restoration rules are part of the [runtime composition contract](../../reference/runtime-composition.md). The
   provider's asynchronous initializer creates missing Cosmos resources when the host starts, not while the callback
   is running.
4. Brooks runtime grains call the reader and writer contracts. Cosmos stores event and cursor documents; Blob leases
   coordinate concurrent writes. An expected version protects an append from an outdated cursor.

The host can choose connection-string-owned composition when it wants the provider package to register lazy keyed SDK
client factories. It can choose host-owned composition when Aspire, dependency injection, or another host layer already
controls the clients. The selected Cosmos key and fixed Brooks Blob key must agree with the host registrations.

Advanced custom providers use the staged native hook rather than a new provider-wide helper:

```csharp
runtime.ConfigureSilo(staged =>
{
    staged.Services.AddSingleton<IBrookStorageProvider, MyBrookStorageProvider>();
    staged.Services.AddSingleton<IBrookStorageReader, MyBrookStorageProvider>();
    staged.Services.AddSingleton<IBrookStorageWriter, MyBrookStorageProvider>();
});
```

This is advanced plumbing. The custom implementation owns its repository, service lifetimes, durability, concurrency,
cancellation, and any external initialization. The runtime hook supplies the staged `ISiloBuilder.Services` surface;
it does not define a generic storage implementation or infer backend behavior.

## Guarantees

- The provider contracts keep Brooks runtime code independent of the storage backend.
- Runtime composition stages provider descriptors before terminal publication; the runtime composition contract
  defines validation, publication, and restoration behavior.
- The Cosmos provider uses its configured database, event-container, lock-container, and `/brookPartitionKey`
  identities. Its client and container service keys are dependency-injection lookup aliases. Exact defaults and
  validation rules are in the [Cosmos reference](cosmos.md).
- Event reads and writes remain asynchronous and accept cancellation tokens through the storage contracts.
- The Cosmos provider uses a bounded Blob lease contract and optimistic expected-version checks as part of its existing
  concurrency behavior.

## Limits

- A storage provider does not provision a production backend merely because its services were staged. The host must
  supply reachable credentials or connection strings and the required keyed clients for host-owned mode.
- Composition does not build a service provider, contact Cosmos or Blob, or prove network connectivity. Resource
  creation and partition-key checks occur through the provider's hosted initializer at host startup.
- A custom provider registered through `ConfigureSilo(...)` receives no automatic repository, retry, durability, or
  migration behavior.
- The storage abstraction does not define cross-provider equivalence. `Format`, failure behavior, partitioning,
  throughput, and durability details remain provider-specific.
- Existing event and cursor data is associated with the configured database, event container, and partition-key
  contract. `LockContainerName` identifies the Blob lock-coordination namespace. Changing a service-key alias alone
  does not move data when it still resolves to the same resources; changing storage identities is a deployment
  migration, not a provider-selection toggle.

## Trade-offs

Runtime composition makes ownership explicit and keeps host-managed clients reusable across features, but it requires
the host and provider to agree on keyed service names. Connection-string-owned mode is convenient for a focused host,
while host-owned mode fits Aspire and applications that centralize client lifetime. The Cosmos provider combines
Cosmos persistence with Blob locking, so operating that deployment requires both services and their corresponding
failure handling.

The explicit custom-provider hook keeps the abstractions assembly lightweight and avoids pretending that arbitrary
providers share the Cosmos lifecycle. It also shifts more responsibility to the application author, who must register
all three contracts and validate the backend behavior.

## Related tasks and reference

- [Cosmos provider reference](cosmos.md) for exact overloads, defaults, options, and failure behavior.
- [Runtime composition](../../reference/runtime-composition.md) for staged services, native Orleans configuration,
  and terminal attachment.
- [Brooks operations](../operations/operations.md) for deployment and identity changes.
- [Brooks troubleshooting](../troubleshooting/troubleshooting.md) for configuration symptoms.
- [Brooks overview](../index.md) for the event-stream layer and adjacent components.
