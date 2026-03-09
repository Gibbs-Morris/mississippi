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
| `LeaseDurationSeconds` | `60` | Distributed lock lease duration |
| `LeaseRenewalThresholdSeconds` | `20` | Threshold before lease renewal |
| `LockContainerName` | `BrookCosmosDefaults.LockContainerId` | Blob container for distributed locking |

## Container Initialization

<!-- Placeholder: document automatic container creation at host startup -->

## Distributed Locking

<!-- Placeholder: document BlobDistributedLockManager and lease configuration -->

## Batching

<!-- Placeholder: document BatchSizeEstimator and transactional batch limits -->

## Operational Notes

<!-- Placeholder: throughput, partitioning, retry behavior, monitoring -->

## Next Steps

- [Storage Providers Overview](index.md)
- [Brooks Operations](../operations/operations.md)
- [Brooks Troubleshooting](../troubleshooting/troubleshooting.md)
