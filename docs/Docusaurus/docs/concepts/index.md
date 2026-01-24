---
sidebar_position: 1
title: Concepts
description: Core concepts and architecture of the Mississippi framework
---

This section explains the foundational concepts behind Mississippi. Read these
pages to understand how the framework works and why it's designed the way it is.

## Architecture

- [System Architecture](./architecture.md) — How components are deployed across
  Client, Server, and Silo tiers
- [Why Mississippi?](./why-mississippi.md) — Benefits compared to traditional
  3-tier architectures

## Core Patterns

Mississippi is built on established patterns:

| Pattern | Description | Mississippi Implementation |
| --- | --- | --- |
| **Event Sourcing** | Store state as a sequence of events | Brooks store immutable event streams |
| **CQRS** | Separate read and write models | Aggregates write, Projections read |
| **Virtual Actors** | Stateful, location-transparent entities | Orleans grains for all components |
| **Unidirectional Data Flow** | State flows one direction | Reservoir's Redux-style pattern |

## Learning Path

**New to event sourcing?** Start with:

1. [Why Mississippi?](./why-mississippi.md) — Understand the benefits
2. [System Architecture](./architecture.md) — See how pieces connect

**Ready to build?** Move to:

1. [Getting Started](../getting-started/index.md) — Installation and setup
2. [Components](../platform/index.md) — Component reference

## Related Topics

- [Start Here](../index.md) — Framework overview
- [Getting Started](../getting-started/index.md) — Installation and first project
