---
sidebar_position: 5
title: Inlet
description: High-level overview of the WASM ↔ ASP.NET bridge for projection subscriptions.
---

# Inlet

Inlet is the WASM-to-ASP.NET bridge that manages UX projection subscriptions, live updates, and client-side projection state built on Reservoir.

## Purpose

- Let clients subscribe to projections by path and entity ID without knowing brook details.
- Manage per-connection projection subscriptions inside Orleans via `IInletSubscriptionGrain`.
- Deduplicate brook stream subscriptions — multiple projections sharing the same brook and entity ID share a single Orleans stream subscription.
- Resolve projection paths to brook names via the projection-brook registry so clients never see brook infrastructure.
- Fan out cursor-move events to all projections that share a brook, notifying SignalR clients with path, entity ID, and new version.
- Deliver projection update notifications over SignalR.
- Coordinate client-side projection state and refresh behavior.

## Where it fits

Inlet sits between UX projections and the Blazor client. On the server, each SignalR connection is backed by an `IInletSubscriptionGrain` that tracks all active subscriptions and listens to brook streams. On the client, Inlet uses SignalR for notifications and Reservoir for client-side projection state management.

## Source code reference

- [InletHub](../../../../src/Inlet.Orleans.SignalR/InletHub.cs) — SignalR hub exposing subscribe/unsubscribe to clients.
- [IInletSubscriptionGrain](../../../../src/Inlet.Orleans/Grains/IInletSubscriptionGrain.cs) — Orleans grain managing subscriptions per connection.
- [InletSubscriptionGrain](../../../../src/Inlet.Orleans/Grains/InletSubscriptionGrain.cs) — implementation with brook stream deduplication and fan-out.
- [IProjectionBrookRegistry](../../../../src/Inlet.Abstractions/IProjectionBrookRegistry.cs) — abstraction for path-to-brook mapping.
- [ProjectionBrookRegistry](../../../../src/Inlet.Orleans/ProjectionBrookRegistry.cs) — runtime registry populated at startup from `[ProjectionPath]` attributes.
- [InletComponent](../../../../src/Inlet.Blazor.WebAssembly/InletComponent.cs) — Blazor base component for projection-aware UX.
