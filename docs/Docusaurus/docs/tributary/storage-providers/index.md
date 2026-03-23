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

## Provider Model

The storage-provider abstraction lives in `Mississippi.Tributary.Runtime.Storage.Abstractions` and defines three contracts:

- `ISnapshotStorageProvider` — unified read/write entry point with a `Format` identifier
- `ISnapshotStorageReader` — read operations for snapshots
- `ISnapshotStorageWriter` — write operations for snapshots

Implementations are registered through extension methods on `IServiceCollection` provided by each provider package.

## Current Coverage

This section now documents the Azure Blob snapshot storage provider in detail and keeps the existing Cosmos page available while its deeper guidance is still being expanded.

## Available Providers

| Provider | Package | Status |
|----------|---------|--------|
| [Azure Blob](blob.md) | `Mississippi.Tributary.Runtime.Storage.Blob` | Available |
| [Cosmos DB](cosmos.md) | `Mississippi.Tributary.Runtime.Storage.Cosmos` | Available |

## Learn More

- [Tributary Overview](../index.md) - Return to the Tributary section landing page
- [Azure Blob Provider](blob.md)
- [Cosmos DB Provider](cosmos.md)
- [Tributary Concepts](../concepts/concepts.md)
- [Tributary Reference](../reference/reference.md)
