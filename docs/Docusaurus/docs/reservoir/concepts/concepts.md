---
title: Reservoir Concepts
sidebar_position: 1
description: Understand Reservoir as Mississippi's client-state management subsystem.
---

# Reservoir Concepts

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Problem This Area Solves

Reservoir exists to keep client-side state transitions explicit, predictable, and testable instead of scattering state changes through UI callbacks and transport glue.

## Core Idea

Reservoir separates state, actions, reducers, selectors, effects, and middleware into a dedicated subsystem.

## How It Fits The Stack

Reservoir can stand alone as a client-state library.

Within Mississippi applications, it often sits beneath [Refraction](../../refraction/index.md) and alongside Inlet-driven client synchronization.

## What This Area Owns

- Store and dispatch pipeline abstractions
- Feature state, actions, reducers, selectors, effects, and middleware
- Client integration for that state-management model

## What This Area Does Not Own

- The Blazor UX component contract itself
- Domain behavior or event-stream persistence

## Trade-Off To Keep In Mind

Reservoir gives you a disciplined state model, but it adds an explicit state-management vocabulary that applications need to adopt consistently.

## Summary

Think of Reservoir as the client-state subsystem, not as the UI layer and not as the domain layer.

## Related Reading

- [Reservoir Getting Started](../getting-started/getting-started.md)
- [Refraction Overview](../../refraction/index.md)
- [Reservoir Reference](../reference/reference.md)