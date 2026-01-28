---
sidebar_position: 1
title: Framework Overview
description: High-level map of Mississippi building blocks, from event sourcing to client UX.
---

# Framework Overview

Mississippi is composed of modular building blocks that move domain changes into live UX state for Blazor WebAssembly and ASP.NET hosts. This section is intentionally high-level; details will be added iteratively.

## Logical order (server → client)

1. [Aggregates](./aggregates.md) — command handling and event production for domain state.
2. [Brooks](./brooks.md) — event streams for append and read access.
3. [UX Projections](./ux-projections.md) — composable read models for UX state.
4. [Aqueduct](./aqueduct.md) — Orleans-backed SignalR backplane for cross-server delivery.
5. [Inlet](./inlet.md) — WASM ↔ ASP.NET bridge for projection subscriptions and delivery.
6. [Reservoir](./reservoir.md) — Redux-style client state container for Blazor/wasm.
7. [Refraction](./refraction.md) — planned component library for Mississippi UX.

## SDK Source Generators

Mississippi includes source generators that reduce boilerplate for aggregates, commands, and projections:

- [GenerateAggregateEndpointsAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateAggregateEndpointsAttribute.cs) — generates silo registration, server controller, and client state for an aggregate.
- [GenerateCommandAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateCommandAttribute.cs) — generates DTOs, mappers, and HTTP endpoints for a command.
- [GenerateProjectionEndpointsAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateProjectionEndpointsAttribute.cs) — generates read-only endpoints and optional client subscriptions for a projection.

## Next steps

Use these pages as a map, then dive into the detailed sections like [Reservoir](../reservoir/index.md).
