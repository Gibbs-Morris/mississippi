---
id: brooks-overview
title: Event Streams (Brooks)
sidebar_label: Overview
sidebar_position: 1
description: Understand Brooks, Mississippi's event-stream and storage foundation.
---

# Event Streams (Brooks)

## Overview

Brooks is the Mississippi event-streaming and event-storage foundation.

It owns the runtime and abstraction surfaces for appending events, reading streams, and plugging in serialization and storage-provider implementations.

## Why This Area Exists

Use Brooks when you need the event-stream layer itself: durable append and read behavior, stream identity, serialization seams, and provider boundaries.

It is the storage and stream foundation beneath higher-level Mississippi domain and projection layers.

## Representative Packages

- `Mississippi.Brooks.Abstractions`
- `Mississippi.Brooks.Runtime`
- `Mississippi.Brooks.Serialization.Abstractions`
- `Mississippi.Brooks.Serialization.Json`

## What This Area Owns

- Event-stream abstractions and runtime services
- Storage-provider contracts for brook persistence
- Serialization seams used by event persistence and retrieval
- Provider-specific implementations such as Cosmos-backed storage and JSON serialization

## How It Fits Mississippi

Brooks can be used independently as the event-stream layer.

Within the full stack, Tributary and Domain Modeling build on top of Brooks to reconstruct state and execute domain behavior.

## Use This Section

Start here when you need to understand the event-stream substrate, not the higher-level domain abstractions built above it.

## Current Coverage

This section now includes typed boundary pages for getting started, concepts, package selection, reference, operations, and troubleshooting.

They establish the stream and provider boundary while deeper storage-provider documentation is still being written.

## Learn More

- [Documentation Home](../index.md) - Return to the product-area docs landing page
- [Brooks Getting Started](./getting-started/getting-started.md) - Start with the event-stream entry points
- [Brooks Concepts](./concepts/concepts.md) - Understand the stream, serialization, and provider boundary
- [Brooks Reference](./reference/reference.md) - Review the currently verified package and ownership surface
- [Brooks Operations](./operations/operations.md) - See the current operational scope and open evidence gaps
- [Tributary](../tributary/index.md) - See the reducers and snapshots layer built above event streams
- [Archived Documentation](../archived/index.md) - Browse the preserved pre-reset docs set
