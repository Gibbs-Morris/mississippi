---
id: tributary-overview
title: Reducers & Snapshots (Tributary)
sidebar_label: Overview
sidebar_position: 1
description: Understand Tributary, Mississippi's reducer and snapshot layer for rebuilding derived state.
---

# Reducers & Snapshots (Tributary)

## Overview

Tributary is the Mississippi layer for reducers and snapshots.

It is responsible for taking event streams and turning them into derived state that can be rebuilt efficiently and persisted as snapshots when needed.

## Why This Area Exists

Use Tributary when you need the state-reduction layer that sits between raw event streams and higher-level domain or read-model behavior.

It exists to keep reducer and snapshot mechanics explicit instead of coupling them to aggregate logic or storage implementations.

## Representative Packages

- `Mississippi.Tributary.Abstractions`
- `Mississippi.Tributary.Runtime`
- `Mississippi.Tributary.Runtime.Storage.Abstractions`
- `Mississippi.Tributary.Runtime.Storage.Cosmos`

## What This Area Owns

- Event reducer abstractions and runtime composition
- Snapshot abstractions and storage-provider seams
- Runtime enforcement around reducer behavior and projection rebuilding

## How It Fits Mississippi

Tributary sits above Brooks and below Domain Modeling.

It provides the state-reconstruction machinery used by projections and other derived models without owning aggregate business behavior itself.

## Use This Section

Start here when you need reducers, snapshots, or the layer that turns Brooks event streams into derived state.

## Current Coverage

This section now includes typed boundary pages for getting started, concepts, package selection, reference, operations, and troubleshooting.

They make the reducer and snapshot boundary explicit while deeper guidance is still being written.

## Learn More

- [Documentation Home](../index.md) - Return to the product-area docs landing page
- [Tributary Getting Started](./getting-started/getting-started.md) - Start with the reducer and snapshot entry points
- [Tributary Concepts](./concepts/concepts.md) - Understand the reduction and snapshot boundary
- [Tributary Reference](./reference/reference.md) - Review the currently verified package and ownership surface
- [Tributary Operations](./operations/operations.md) - See the current operational scope and open evidence gaps
- [Brooks](../brooks/index.md) - See the underlying event stream layer
- [Domain Modeling](../domain-modeling/index.md) - See the domain behavior layer that builds above Tributary
