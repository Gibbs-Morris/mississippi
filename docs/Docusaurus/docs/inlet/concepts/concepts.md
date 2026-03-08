---
title: Inlet Concepts
sidebar_position: 1
description: Understand Inlet as Mississippi's composition and source-generation layer.
---

# Inlet Concepts

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Problem This Area Solves

Inlet exists to keep client, gateway, and runtime surfaces aligned so teams do not have to rebuild the same transport and projection surface by hand.

## Core Idea

Source generators and runtime packages work together so projection DTOs, APIs, registrations, and subscription wiring stay synchronized.

## How It Fits The Stack

Inlet is the composition layer that ties together [Aqueduct](../../aqueduct/index.md), [Reservoir](../../reservoir/index.md), and [Domain Modeling](../../domain-modeling/index.md).

## What This Area Owns

- Shared abstractions for projection paths and related metadata
- Client support for projection state and subscriptions
- Gateway support for generated APIs and SignalR delivery
- Runtime support for discovery and generated registrations
- Source generators that align those layers

## What This Area Does Not Own

- The underlying real-time backplane by itself
- The domain behavior layer by itself
- The client-state model by itself

## Trade-Off To Keep In Mind

Inlet reduces duplicated cross-layer plumbing, but it only makes sense when the problem truly spans multiple layers rather than one subsystem in isolation.

## Summary

Think of Inlet as the layer that keeps Mississippi surfaces aligned across the stack.

## Related Reading

- [Inlet Getting Started](../getting-started/getting-started.md)
- [Spring Sample](../../samples/spring-sample/index.md)
- [Inlet Reference](../reference/reference.md)