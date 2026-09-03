---
id: reservoir-overview
title: Client State (Reservoir)
sidebar_label: Overview
sidebar_position: 1
description: Understand Reservoir, Mississippi's Redux-style client state-management layer.
slug: /subsystems/reservoir
---

# Client State (Reservoir)

## Overview

Reservoir is the Mississippi client state-management library.

It provides the store, actions, reducers, selectors, effects, middleware, and UI integration patterns used to keep client-side state predictable and testable.

## Why This Area Exists

Use Reservoir when client behavior needs explicit state transitions, repeatable side-effect handling, and selectors or tests that are independent of UI rendering details.

It gives Mississippi a dedicated client-state model instead of spreading state changes across components, callbacks, and transport glue.

## Representative Packages

- `Mississippi.Reservoir.Abstractions`
- `Mississippi.Reservoir.Core`
- `Mississippi.Reservoir.Client`
- `Mississippi.Reservoir.TestHarness`

## What This Area Owns

- Store and dispatch pipeline abstractions
- Feature state, actions, reducers, selectors, effects, and middleware
- Blazor integration for applications that want Redux-style state management

## How It Fits Mississippi

Reservoir can be used independently of the rest of the framework.

Within the broader stack, it is commonly used beneath Refraction and Inlet-powered clients to manage local and synchronized client state.

## Use This Section

Start here when you need the client-state model itself: store behavior, reducers, effects, selectors, middleware, or the Blazor integration surface.

## Current Coverage

This section includes getting-started, concepts, package-selection, reference,
and troubleshooting guidance for the currently verified Reservoir surface.

## Learn More

- [Documentation Home](../../index.md) - Return to the product-area docs landing page
- [Reservoir Getting Started](getting-started.md) - Start with the state-management entry points
- [Reservoir Concepts](concepts.md) - Understand the store, reducer, and effect boundary
- [Reservoir Reference](reference.md) - Review the currently verified package and ownership surface
- [Refraction](../refraction/index.md) - See the Blazor UX layer that can sit on top of Reservoir
