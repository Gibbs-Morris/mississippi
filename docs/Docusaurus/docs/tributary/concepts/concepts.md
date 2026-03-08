---
title: Tributary Concepts
sidebar_position: 1
description: Understand Tributary as Mississippi's reducer and snapshot layer.
---

# Tributary Concepts

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Problem This Area Solves

Tributary exists to turn event streams into derived state without forcing reducer and snapshot mechanics into the stream layer or the domain layer.

## Core Idea

Tributary is the reconstruction layer between raw streams and higher-level behavior.

## How It Fits The Stack

Tributary sits above [Brooks](../../brooks/index.md) and below [Domain Modeling](../../domain-modeling/index.md).

## What This Area Owns

- Event reducer abstractions and runtime composition
- Snapshot abstractions and storage-provider seams
- Runtime support for rebuilding derived state efficiently

## What This Area Does Not Own

- Raw event-stream persistence
- Aggregate, saga, or projection business behavior

## Trade-Off To Keep In Mind

Tributary gives you an explicit reconstruction layer, but it is a supporting layer rather than the place where business behavior is authored.

## Summary

Think of Tributary as the bridge between event streams and domain-facing behavior.

## Related Reading

- [Tributary Getting Started](../getting-started/getting-started.md)
- [Brooks Overview](../../brooks/index.md)
- [Tributary Reference](../reference/reference.md)