---
id: tributary-storage-providers
title: Tributary Storage Providers
sidebar_label: Overview
sidebar_position: 1
description: Storage provider model for snapshot persistence in Mississippi.
---

# Tributary Storage Providers

## Overview

Tributary uses a pluggable storage-provider model for snapshot persistence. Each provider implements the `ISnapshotStorageProvider` interface, which combines read and write operations and identifies itself with a `Format` string.

The Spring sample now uses the Azure Blob provider for snapshots while Brooks event streams remain on Cosmos DB.

## Provider Model

The storage-provider abstraction lives in `Mississippi.Tributary.Runtime.Storage.Abstractions` and defines three contracts:

- `ISnapshotStorageProvider` — unified read/write entry point with a `Format` identifier
- `ISnapshotStorageReader` — read operations for snapshots
- `ISnapshotStorageWriter` — write operations for snapshots

Implementations are registered through extension methods on `IServiceCollection` provided by each provider package.

## Available Providers

| Provider | Package | Status |
|----------|---------|--------|
| [Azure Blob Storage](blob-storage.md) | `Mississippi.Tributary.Runtime.Storage.Blobs` | Available |
| [Cosmos DB](cosmos.md) | `Mississippi.Tributary.Runtime.Storage.Cosmos` | Available |

## Learn More

- [Tributary Overview](../index.md) - Return to the Tributary section landing page
- [Azure Blob Storage Provider](blob-storage.md)
- [Cosmos DB Provider](cosmos.md)
- [Tributary Concepts](../concepts/concepts.md)
- [Tributary Reference](../reference/reference.md)
