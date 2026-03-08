---
id: home
title: Documentation
sidebar_label: Home
sidebar_position: 1
description: Understand what Mississippi is for, choose the right entry point, and find the subsystem, package, or sample you need.
slug: /
---

# Mississippi Documentation

## Overview

Mississippi helps teams model domain behavior directly and let the framework scaffold much of the surrounding API, client, and runtime plumbing.

You define aggregates, commands, events, and projections. Mississippi then composes event sourcing, CQRS, Orleans execution, generated APIs, client actions, and real-time projection delivery into one system.

Use this landing page to choose the right entry point: start with concepts if you are new to the framework, jump to a subsystem if you need a specific capability, or open samples if you want to see a complete application.

## Start Here

| If you want to... | Start here |
| --- | --- |
| Understand what Mississippi is for and how the pieces fit together | [Concepts](./concepts/index.md) |
| Evaluate or adopt a specific subsystem | The product-area sections below |
| Find package names for independently adoptable areas | [Package Entry Points](#package-entry-points) |
| See a complete working application | [Samples](./samples/index.md) |

## Independent Foundations

These areas can be adopted on their own without requiring the rest of the Mississippi stack.

| Area | What it covers |
| --- | --- |
| [Aqueduct](./aqueduct/index.md) | Orleans-backed SignalR backplane support for distributed real-time messaging |
| [Refraction](./refraction/index.md) | Blazor UX components built around a state-down, events-up interaction model |
| [Reservoir](./reservoir/index.md) | Redux-style client state management |
| [Brooks](./brooks/index.md) | Event streams and event storage |

## Package Entry Points

These independent areas also expose package entry points that can be adopted without taking the full Mississippi stack.

The package IDs below come from the packable projects under `src/` and use the repository-wide `Mississippi.` package prefix.

| Area | Representative packages | Notes |
| --- | --- | --- |
| [Aqueduct](./aqueduct/index.md) | `Mississippi.Aqueduct.Abstractions`, `Mississippi.Aqueduct.Gateway`, `Mississippi.Aqueduct.Runtime` | Public contracts plus gateway and Orleans runtime integrations for the backplane |
| [Refraction](./refraction/index.md) | `Mississippi.Refraction.Abstractions`, `Mississippi.Refraction.Client`, `Mississippi.Refraction.Client.StateManagement` | Blazor UX contracts, runtime components, and page-composition helpers |
| [Reservoir](./reservoir/index.md) | `Mississippi.Reservoir.Abstractions`, `Mississippi.Reservoir.Core`, `Mississippi.Reservoir.Client`, `Mississippi.Reservoir.TestHarness` | State-management contracts, runtime, Blazor integration, and testing support |
| [Brooks](./brooks/index.md) | `Mississippi.Brooks.Abstractions`, `Mississippi.Brooks.Runtime`, `Mississippi.Brooks.Serialization.Abstractions`, `Mississippi.Brooks.Serialization.Json` | Event-streaming contracts, runtime, and serialization seams |

Use the area pages above when you want the architectural view. Use the package names in this table when you are deciding what to reference from an application or library.

## Domain Layers

These areas build domain behavior and derived state on top of the lower event and storage primitives.

| Area | What it covers |
| --- | --- |
| [Tributary](./tributary/index.md) | Reducers and snapshots |
| [Domain Modeling](./domain-modeling/index.md) | Aggregates, sagas, and UX projections |

## Composition Layer

This area keeps the client, gateway, and runtime surfaces aligned.

| Area | What it covers |
| --- | --- |
| [Inlet](./inlet/index.md) | Source-generated alignment across client, HTTP, and runtime layers |

## Samples

This section covers complete applications that show how multiple Mississippi areas compose in practice.

| Area | What it covers |
| --- | --- |
| [Samples](./samples/index.md) | End-to-end sample applications, guided walkthroughs, and sample-specific validation paths |

## Legacy Material

Earlier documentation is still available under [Archived Documentation](./archived/index.md).

Use that material when you need preserved reference pages from the older docs set, but prefer the active top-level sections for new navigation.

## Learn More

- [Concepts](./concepts/index.md) - Start with the framework overview and mental model
- [Aqueduct](./aqueduct/index.md) - Start with the Orleans-backed SignalR backplane section
- [Refraction](./refraction/index.md) - Start with the Blazor UX component section
- [Reservoir](./reservoir/index.md) - Start with the client state-management section
- [Brooks](./brooks/index.md) - Start with the event stream and storage section
- [Tributary](./tributary/index.md) - Start with the reducers and snapshots section
- [Domain Modeling](./domain-modeling/index.md) - Start with the aggregates, sagas, and projections section
- [Inlet](./inlet/index.md) - Start with the composition and source-generation section
- [Samples](./samples/index.md) - Explore complete applications built with Mississippi
- [Archived Documentation](./archived/index.md) - Browse the preserved pre-reset docs set
