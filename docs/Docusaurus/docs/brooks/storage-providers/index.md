---
id: brooks-storage-providers
title: Brooks Storage Providers
sidebar_label: Overview
sidebar_position: 1
description: Storage provider model for brook event persistence in Mississippi.
---

# Brooks Storage Providers

:::warning
This section contains placeholder content. Detailed guidance for each provider is still being written.
:::

## Overview

Brooks uses a pluggable storage-provider model for event persistence. Each provider implements the `IBrookStorageProvider` interface, which combines read and write operations and identifies itself with a `Format` string.

## Provider Model

The storage-provider abstraction lives in `Mississippi.Brooks.Runtime.Storage.Abstractions` and defines three contracts:

- `IBrookStorageProvider` — unified read/write entry point with a `Format` identifier
- `IBrookStorageReader` — read operations for event streams
- `IBrookStorageWriter` — write operations for event streams

Implementations are registered through extension methods on `IServiceCollection` provided by each provider package.

## Available Providers

| Provider | Package | Status |
|----------|---------|--------|
| [Cosmos DB](cosmos.md) | `Mississippi.Brooks.Runtime.Storage.Cosmos` | Available |

## Learn More

- [Brooks Overview](../index.md) - Return to the Brooks section landing page
- [Cosmos DB Provider](cosmos.md)
- [Brooks Concepts](../concepts/concepts.md)
- [Brooks Reference](../reference/reference.md)
