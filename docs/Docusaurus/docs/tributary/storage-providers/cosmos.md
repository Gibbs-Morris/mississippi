---
id: tributary-storage-cosmos
title: "Tributary: Cosmos DB Provider"
sidebar_label: Cosmos DB
sidebar_position: 2
description: Cosmos DB storage provider for snapshot persistence.
---

# Tributary: Cosmos DB Provider

:::warning
This page contains placeholder content. Full configuration, operational, and troubleshooting guidance is still being written.
:::

## Overview

The Cosmos DB snapshot storage provider persists snapshot envelopes to Azure Cosmos DB containers.

## Packages

- `Mississippi.Tributary.Runtime.Storage.Cosmos`
- `Mississippi.Tributary.Runtime.Storage.Abstractions`

## Registration

Register the provider with `AddCosmosSnapshotStorageProvider()` on `IServiceCollection`.

## Configuration Options

Configuration is provided through `SnapshotStorageOptions`:

| Option | Default | Description |
|--------|---------|-------------|
| `ContainerId` | `SnapshotCosmosDefaults.ContainerId` | Cosmos DB container for snapshots |
| `DatabaseId` | `SnapshotCosmosDefaults.DatabaseId` | Cosmos DB database identifier |
| `CosmosClientServiceKey` | `SnapshotCosmosDefaults.CosmosClientServiceKey` | Keyed service key for resolving `CosmosClient` |
| `QueryBatchSize` | `100` | Batch size for snapshot queries |

## Container Initialization

<!-- Placeholder: document automatic container creation via CosmosContainerInitializer hosted service -->

## Diagnostics

<!-- Placeholder: document diagnostic surfaces in Tributary.Runtime.Storage.Cosmos.Diagnostics -->

## Operational Notes

<!-- Placeholder: throughput, partitioning, retry behavior, monitoring -->

## Next Steps

- [Storage Providers Overview](index.md)
- [Tributary Operations](../operations/operations.md)
- [Tributary Troubleshooting](../troubleshooting/troubleshooting.md)
