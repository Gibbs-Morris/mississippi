---
title: Brooks Concepts
sidebar_position: 1
description: Understand Brooks as Mississippi's event-stream and provider foundation.
---

# Brooks Concepts

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

## What This Page Guarantees

- It defines Brooks as the event-stream substrate and provider-seam boundary in Mississippi.
- It identifies the layers above Brooks that readers should move to when the problem is no longer the stream foundation itself.

## What This Page Does Not Claim

- Storage-provider guarantees, serializer guarantees, or compatibility guarantees
- Full provider configuration or deployment guidance
- Detailed failure-mode or performance documentation

## Trade-Off To Keep In Mind

Brooks gives you a clean stream foundation, but applications normally need higher-level layers above it to express domain behavior and derived state.

## Summary

Think of Brooks as the event-stream foundation underneath the rest of the Mississippi stack.

## Related Reading

- [Brooks Getting Started](../getting-started/getting-started.md)
- [Tributary Overview](../../tributary/index.md)
- [Brooks Reference](../reference/reference.md)
