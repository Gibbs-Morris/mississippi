---
sidebar_position: 4
title: UX Projections
description: High-level overview of composable UX projections for read-optimized state.
---

# UX Projections

UX projections are composable, read-optimized views of event streams designed for client UX state. They are exposed via HTTP and signaled via streams when updated.

## Purpose

- Provide small, composable projection models for UX state.
- Support versioned reads and cache-friendly access patterns.
- Publish change notifications without pushing full payloads.

## Where it fits

UX projections consume brooks and feed client-facing state via Inlet and Reservoir. They are the read-model layer for the UI.

## Source code reference

- [IUxProjectionGrain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.UxProjections.Abstractions/IUxProjectionGrain.cs)
- [GenerateProjectionEndpointsAttribute](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateProjectionEndpointsAttribute.cs) — marks a projection for endpoint code generation
- [UxProjectionChangedEvent](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.UxProjections.Abstractions/Subscriptions/UxProjectionChangedEvent.cs)
