---
sidebar_position: 6
title: Reservoir
description: High-level overview of the Redux-style state container for Blazor/wasm apps.
---

# Reservoir

Reservoir is Mississippi’s Redux-style state management framework for Blazor WebAssembly and Blazor Server apps. It provides a predictable store, actions, reducers, and effects.

## Purpose

- Centralize client state with a single store.
- Dispatch actions that reducers and effects can handle.
- Keep UI components subscribed to state changes.

## Where it fits

Reservoir powers client-side state and is used by Inlet to manage projection state and updates in the UI.

## Source code reference

- [IStore](../../../../src/Reservoir.Abstractions/IStore.cs)
- [StoreComponent](../../../../src/Reservoir.Blazor/StoreComponent.cs)
