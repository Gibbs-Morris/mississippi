---
id: domain-modeling-overview
title: Domain Behavior (Domain Modeling)
sidebar_label: Overview
sidebar_position: 1
description: Understand Domain Modeling, Mississippi's domain behavior layer for aggregates, sagas, effects, and UX projections.
---

# Domain Behavior (Domain Modeling)

## Overview

Domain Modeling is the Mississippi area where aggregates, sagas, event effects, UX projections, and projection replication sinks are expressed.

It defines the domain-facing abstractions and runtime pieces that let application code focus on business rules instead of infrastructure plumbing.

## Why This Area Exists

Use Domain Modeling when you want Mississippi's domain-facing building blocks rather than the lower-level stream, reducer, or transport subsystems.

This is where application code expresses commands, aggregate behavior, sagas, effects, UX projections, and projection-derived external read models in business terms.

## Representative Packages

- `Mississippi.DomainModeling.Abstractions`
- `Mississippi.DomainModeling.Runtime`
- `Mississippi.DomainModeling.Gateway`
- `Mississippi.DomainModeling.TestHarness`
- `Mississippi.DomainModeling.ReplicaSinks.Abstractions`
- `Mississippi.DomainModeling.ReplicaSinks.Runtime`
- `Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions`
- `Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos`

## What This Area Owns

- Aggregate command-handling abstractions and runtime support
- Saga orchestration and compensation surfaces
- Event-effect patterns attached to domain behavior
- UX projection grain abstractions and runtime support
- Projection replication sink metadata, latest-state delivery orchestration, and bounded operator workflows for external read-model copies

## How It Fits Mississippi

Domain Modeling sits above Brooks and Tributary.

It is the place where event streams and state reconstruction are turned into concrete domain behavior, projection-facing models, and projection-derived external replicas.

## Use This Section

Start here when the question is about aggregates, sagas, effects, UX projections, or projection replication sinks rather than about the lower-level mechanics underneath them.

## Current Coverage

This section now includes typed boundary pages for getting started, concepts, package selection, reference, and troubleshooting.

Projection replication sinks also have focused concept, how-to, and reference pages for the shipped single-instance latest-state slice.

## Learn More

- [Documentation Home](../index.md) - Return to the product-area docs landing page
- [Domain Modeling Getting Started](./getting-started/getting-started.md) - Start with the domain-facing entry points
- [Domain Modeling Concepts](./concepts/concepts.md) - Understand the aggregate, saga, and projection boundary
- [Projection Replication Sinks](./concepts/projection-replication-sinks.md) - Learn how named projection replication sinks fit into Domain Modeling
- [How to configure projection replication sinks](./how-to/configure-projection-replication-sinks.md) - Set up the supported replica-sink onboarding path
- [Domain Modeling Reference](./reference/reference.md) - Review the currently verified package and ownership surface
- [Projection replication sinks reference](./reference/projection-replication-sinks.md) - Look up the replica-sink contracts, defaults, and current slice limits
- [Tributary](../tributary/index.md) - See the reducers and snapshots layer beneath domain behavior
- [Archived Concepts](../archived/concepts/index.md) - Browse the preserved concept material
