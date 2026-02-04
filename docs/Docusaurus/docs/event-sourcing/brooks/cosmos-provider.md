---
id: cosmos-provider
title: Cosmos DB Provider
sidebar_label: Cosmos DB Provider
sidebar_position: 6
description: Configure the Azure Cosmos DB storage provider for brook event streams.
---

# Cosmos DB Provider

## Overview

The Cosmos DB provider implements `IBrookStorageProvider` using Azure Cosmos DB for NoSQL. It provides scalable, globally distributed event storage with automatic indexing and change-feed integration.

This page focuses on **Public API / Developer Experience** for configuring and using the Cosmos DB storage provider.

## Installation

```bash
dotnet add package Mississippi.EventSourcing.Brooks.Cosmos
```

## Registration

### Basic Setup

```csharp
using Mississippi.EventSourcing.Brooks.Cosmos;

services.AddCosmosBrookStorageProvider();
```

This registers all required services and starts a hosted service that creates containers on startup.

### CosmosClient Registration

The provider requires a keyed `CosmosClient` registration. Use the Aspire integration or register manually:

```csharp
// Option 1: Aspire integration (recommended)
builder.AddAzureCosmosClient("cosmos-brooks");

// Option 2: Manual registration
services.AddKeyedSingleton<CosmosClient>(
    MississippiDefaults.ServiceKeys.CosmosBrooksClient,
    (sp, _) => new CosmosClient(connectionString));
```

### Blob Storage for Locking

The provider uses Azure Blob Storage for distributed locking:

```csharp
// Register keyed BlobServiceClient for locking
services.AddKeyedSingleton<BlobServiceClient>(
    MississippiDefaults.ServiceKeys.BlobLocking,
    (sp, _) => new BlobServiceClient(connectionString));
```

## Configuration

Configure options via `BrookStorageOptions`:

```csharp
services.Configure<BrookStorageOptions>(options =>
{
    options.DatabaseId = "my-database";
    options.ContainerId = "my-brooks";
    options.MaxEventsPerBatch = 50;
});
```

Or bind from configuration:

```csharp
services.Configure<BrookStorageOptions>(
    configuration.GetSection("Mississippi:Brooks:Cosmos"));
```

### Options Reference

| Property | Default | Description |
|----------|---------|-------------|
| `DatabaseId` | `"mississippi"` | Cosmos DB database identifier. |
| `ContainerId` | `"brooks"` | Container for storing events. |
| `CosmosClientServiceKey` | `"CosmosBrooksClient"` | Keyed service key for `CosmosClient`. |
| `LockContainerName` | `"locks"` | Blob container for distributed locking. |
| `LeaseDurationSeconds` | `60` | Lease expiration for locks. |
| `LeaseRenewalThresholdSeconds` | `20` | Threshold before renewing a lease. |
| `MaxEventsPerBatch` | `90` | Events per transactional batch (max 100). |
| `MaxRequestSizeBytes` | `1,700,000` | Maximum payload size (under 2 MB limit). |
| `QueryBatchSize` | `100` | Items per query page. |

## Container Model

Events are stored as individual documents with the brook key as the partition key:

```json
{
  "id": "evt_00000001",
  "pk": "MYAPP.ORDERS.ORDER-123",
  "eventType": "OrderPlaced",
  "source": "MYAPP.ORDERS.ORDER-123",
  "time": "2024-01-15T10:30:00Z",
  "dataContentType": "application/json",
  "data": "base64-encoded-payload",
  "position": 1
}
```

A cursor document tracks the latest position:

```json
{
  "id": "cursor",
  "pk": "MYAPP.ORDERS.ORDER-123",
  "position": 42,
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

## Transactional Batches

The provider uses Cosmos DB transactional batches to append events atomically:

```mermaid
flowchart LR
    A[Acquire Blob Lease] --> B[Read Cursor]
    B --> C[Create Batch]
    C --> D[Add Event Docs]
    D --> E[Upsert Cursor]
    E --> F[Execute Batch]
    F --> G[Release Lease]
```

### Batch Limits

Cosmos DB limits transactional batches to:

- **100 operations** per batch
- **2 MB** total request size

The provider defaults to 90 events per batch with 1.7 MB size limit, leaving headroom for cursor operations and metadata.

## Optimistic Concurrency

Pass `expectedVersion` to enforce optimistic concurrency:

```csharp
await provider.AppendEventsAsync(
    brookKey,
    events,
    expectedVersion: new BrookPosition(10));
```

The provider verifies the cursor position matches before writing. On conflict, it throws `OptimisticConcurrencyException`.

## Recovery Service

The provider includes `IBrookRecoveryService` for handling poisoned streams:

```csharp
IBrookRecoveryService recovery = services.GetRequiredService<IBrookRecoveryService>();

// Truncate events beyond a known-good position
await recovery.TruncateAsync(brookKey, afterPosition: new BrookPosition(50));

// Rebuild cursor from events
await recovery.RebuildCursorAsync(brookKey);
```

## Hosted Initialization

`CosmosContainerInitializer` runs on host startup to ensure containers exist:

```csharp
// Automatic—no action needed after AddCosmosBrookStorageProvider()
// The hosted service creates missing containers with partition key "/pk"
```

## Summary

The Cosmos DB provider delivers scalable event storage with transactional guarantees. Configure connection details, register keyed services for Cosmos and Blob clients, and tune batch settings for your workload.

## Next Steps

- [Storage Providers](./storage-providers.md) - Overview of the storage abstraction.
- [Reading and Writing](./reading-and-writing.md) - Grain-based event operations.
