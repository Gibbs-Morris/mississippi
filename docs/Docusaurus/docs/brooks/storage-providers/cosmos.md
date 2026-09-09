---
id: brooks-storage-cosmos
title: "Brooks: Cosmos DB Provider"
sidebar_label: Cosmos DB
sidebar_position: 2
description: Cosmos DB storage provider for brook event persistence.
---

# Brooks: Cosmos DB Provider

:::warning
This page contains placeholder content. Full configuration, operational, and troubleshooting guidance is still being written.
:::

## Overview

The Cosmos DB brook storage provider persists event streams to Azure Cosmos DB containers and uses Azure Blob Storage for distributed locking.

## Packages

- `Mississippi.Brooks.Runtime.Storage.Cosmos`
- `Mississippi.Brooks.Runtime.Storage.Abstractions`

## Registration

Register the provider with `AddCosmosBrookStorageProvider()` on `IServiceCollection`.

## Configuration Options

Configuration is provided through `BrookStorageOptions`:

| Option | Default | Description |
|--------|---------|-------------|
| `ContainerId` | `BrookCosmosDefaults.ContainerId` | Cosmos DB container for event storage |
| `DatabaseId` | `BrookCosmosDefaults.DatabaseId` | Cosmos DB database identifier |
| `CosmosClientServiceKey` | `BrookCosmosDefaults.CosmosClientServiceKey` | Keyed service key for resolving `CosmosClient` |
| `MaxEventsPerBatch` | `90` | Maximum events per transactional batch |
| `MaxRequestSizeBytes` | `1_700_000` | Maximum request size in bytes for Cosmos DB operations |
| `QueryBatchSize` | `100` | Batch size for query operations |
| `LeaseDurationSeconds` | `60` | Distributed lock lease duration |
| `LeaseRenewalThresholdSeconds` | `20` | Threshold before lease renewal |
| `LockContainerName` | `BrookCosmosDefaults.LockContainerId` | Blob container for distributed locking |

## Container Initialization

<!-- Placeholder: document automatic container creation at host startup -->

## Distributed Locking

<!-- Placeholder: document BlobDistributedLockManager and lease configuration -->

## Batching

Large appends write their event batches before advancing the committed cursor. Cursor commitment also removes pending-cursor metadata, so a failure from that operation can occur after the cursor has already advanced.

Once a cursor commit has been attempted, the writer does not attempt compensating deletes in response to its exception. The commit operation may already have removed pending metadata before reporting failure. The writer propagates the original failure and leaves appended events and any remaining pending metadata intact. Callers still need authoritative recovery to distinguish a committed cursor from an unknown outcome; an exception alone is not proof that the append did not commit.

## Operational Notes

<!-- Placeholder: throughput, partitioning, retry behavior, monitoring -->

## Summary

The Cosmos DB brook storage provider handles event persistence, distributed locking, and batched writes for Brooks event streams. Detailed operational and configuration guidance is pending.

## Next Steps

- [Storage Providers Overview](index.md)
- [Brooks Operations](../operations/operations.md)
- [Brooks Troubleshooting](../troubleshooting/troubleshooting.md)
