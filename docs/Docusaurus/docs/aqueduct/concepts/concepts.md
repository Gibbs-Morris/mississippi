---
title: Aqueduct Concepts
sidebar_position: 1
description: Understand Aqueduct as Mississippi's Orleans-backed SignalR backplane layer.
---

# Aqueduct Concepts

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Problem This Area Solves

Aqueduct exists for the case where Orleans-driven events and real-time delivery must work across multiple hosts without forcing application code to manage SignalR backplane mechanics directly.

## Core Idea

Aqueduct separates distributed SignalR routing concerns into a dedicated backplane layer with clear gateway and runtime package boundaries.

## How It Fits The Stack

Aqueduct is infrastructure. It is not the domain layer, the client state layer, or the source-generation layer.

Within Mississippi, Inlet can compose with Aqueduct for real-time projection delivery, but Aqueduct remains the underlying backplane concern.

## What This Area Owns

- Orleans-backed SignalR backplane integration
- Orleans-driven push delivery of events and notifications into SignalR-connected clients
- Gateway-side hub lifetime and notifier registration concerns
- Runtime-side backplane registration concerns

## What This Area Does Not Own

- Aggregate, saga, or projection behavior
- Client-side state management
- Full generated API and subscription alignment across the stack

## Trade-Off To Keep In Mind

Aqueduct gives you a dedicated infrastructure boundary, but it is only one layer of the full delivery story. Higher-level application questions often continue in Inlet or Domain Modeling.

## Summary

Think of Aqueduct as the distributed real-time transport boundary in Mississippi.

## Related Reading

- [Aqueduct Getting Started](../getting-started/getting-started.md)
- [Inlet Overview](../../inlet/index.md)
- [Aqueduct Reference](../reference/reference.md)