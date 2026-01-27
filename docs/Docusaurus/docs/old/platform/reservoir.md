---
sidebar_position: 7
title: Reservoir
description: High-level overview of the Redux-style state container for Blazor/wasm apps.
---

# Reservoir

Reservoir is Mississippi's Redux-style state management framework for Blazor WebAssembly and Blazor Server apps. It provides a predictable store, actions, reducers, and action effects.

## Purpose

- Centralize client state with a single store.
- Dispatch actions that reducers and action effects can handle.
- Keep UI components subscribed to state changes.

## Where it fits

Reservoir powers client-side state and is used by Inlet to manage projection state and updates in the UI.

## Source code reference

- [IStore](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Abstractions/IStore.cs)
- [StoreComponent](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Blazor/StoreComponent.cs)
