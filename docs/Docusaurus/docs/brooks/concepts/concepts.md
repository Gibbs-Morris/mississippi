---
title: Brooks Concepts
sidebar_position: 1
description: Understand Brooks as Mississippi's event-stream and provider foundation.
---

# Brooks Concepts

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Problem This Area Solves

Brooks exists to make event streams, serialization seams, and storage-provider boundaries explicit instead of burying them inside higher-level domain code.

## Core Idea

Brooks is the stream substrate. Layers above it consume streams, but Brooks owns the underlying append, read, serialization, and provider seams.

## How It Fits The Stack

Brooks sits below [Tributary](../../tributary/index.md) and [Domain Modeling](../../domain-modeling/index.md).

## What This Area Owns

- Event-stream abstractions and runtime services
- Storage-provider contracts for brook persistence
- Serialization seams used by event persistence and retrieval

## What This Area Does Not Own

- Reducers and snapshots
- Aggregate, saga, or projection behavior

## Trade-Off To Keep In Mind

Brooks gives you a clean stream foundation, but applications normally need higher-level layers above it to express domain behavior and derived state.

## Summary

Think of Brooks as the event-stream foundation underneath the rest of the Mississippi stack.

## Related Reading

- [Brooks Getting Started](../getting-started/getting-started.md)
- [Tributary Overview](../../tributary/index.md)
- [Brooks Reference](../reference/reference.md)