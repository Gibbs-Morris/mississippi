---
id: tributary-storage-providers
title: Tributary Storage Providers
sidebar_label: Overview
sidebar_position: 1
description: Storage provider model for snapshot persistence in Mississippi.
---

# Tributary Storage Providers

:::warning
This section contains placeholder content. Detailed guidance for each provider is still being written.
:::

## Overview

Tributary uses a pluggable storage-provider model for snapshot persistence. Each provider implements the `ISnapshotStorageProvider` interface, which combines read and write operations and identifies itself with a `Format` string.

## Provider Model

The storage-provider abstraction lives in `Mississippi.Tributary.Runtime.Storage.Abstractions` and defines three contracts:

- `ISnapshotStorageProvider` — unified read/write entry point with a `Format` identifier
- `ISnapshotStorageReader` — read operations for snapshots
- `ISnapshotStorageWriter` — write operations for snapshots

Implementations are registered through extension methods on `IServiceCollection` provided by each provider package.

## Available Providers

| Provider | Package | Status |
|----------|---------|--------|
| [Cosmos DB](cosmos.md) | `Mississippi.Tributary.Runtime.Storage.Cosmos` | Available |

## Learn More

- [Tributary Overview](../index.md) - Return to the Tributary section landing page
- [Cosmos DB Provider](cosmos.md)
- [Tributary Concepts](../concepts/concepts.md)
- [Tributary Reference](../reference/reference.md)
