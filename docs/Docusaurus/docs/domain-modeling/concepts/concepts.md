---
title: Domain Modeling Concepts
sidebar_position: 1
description: Understand Domain Modeling as Mississippi's domain-facing aggregate, saga, effect, and UX projection layer.
---

# Domain Modeling Concepts

## Problem This Area Solves

Domain Modeling exists to let application code express business behavior directly instead of encoding it in lower-level stream, transport, or client infrastructure.

## Core Idea

This is the layer where aggregates, sagas, event effects, and UX projections are described in business terms.

## How It Fits The Stack

Domain Modeling sits above [Brooks](../../brooks/index.md) and [Tributary](../../tributary/index.md).

## What This Area Owns

- Aggregate command-handling abstractions and runtime support
- Saga orchestration surfaces
- Event-effect patterns attached to domain behavior
- UX projection abstractions and runtime support

## What This Area Does Not Own

- Raw event-stream persistence
- The reducer and snapshot substrate beneath domain behavior
- Client-side UI composition as a subsystem

## What This Page Guarantees

- It defines Domain Modeling as the business-facing layer for aggregates, sagas, effects, and UX projections.
- It identifies the lower layers readers should switch to when the problem is really stream or reduction infrastructure.

## What This Page Does Not Claim

- Full aggregate, saga, effect, or projection API reference
- Orchestration, durability, retry, or ordering guarantees
- Detailed runtime behavior documentation for every domain pattern

## Trade-Off To Keep In Mind

Domain Modeling gives you business-facing building blocks, but it depends on the lower layers remaining conceptually separate and well understood.

## Summary

Think of Domain Modeling as the place where Mississippi becomes application behavior instead of infrastructure.

## Related Reading

- [Domain Modeling Getting Started](../getting-started/getting-started.md)
- [Tributary Overview](../../tributary/index.md)
- [Domain Modeling Reference](../reference/reference.md)
