---
id: inlet-overview
title: Inlet
sidebar_label: Overview
sidebar_position: 1
description: Inlet keeps Mississippi client, HTTP, and runtime surfaces aligned through source generation and runtime wiring.
---

# Inlet

## Overview

Inlet is the Mississippi composition layer.

It uses source generators plus client, gateway, and runtime packages to keep projection DTOs, generated HTTP endpoints, runtime registrations, and client subscription wiring aligned across the full stack.

## Why This Area Exists

Use Inlet when the problem is alignment across client, HTTP, and runtime surfaces rather than any one layer in isolation.

It exists to keep generated contracts and runtime wiring synchronized so teams can model the domain once and avoid rebuilding the same transport and projection surface repeatedly.

## Representative Packages

- `Mississippi.Inlet.Abstractions`
- `Mississippi.Inlet.Client`
- `Mississippi.Inlet.Gateway`
- `Mississippi.Inlet.Runtime`

## What This Area Owns

- Shared abstractions for projection paths and related metadata
- Client runtime support for projection state and subscriptions
- Gateway runtime support for generated APIs and SignalR delivery
- Runtime registration support for projection discovery and generated silo wiring
- Source generators that produce aligned code across those layers

## How It Fits Mississippi

Inlet is the layer that ties the rest of the stack together.

It composes with Aqueduct for real-time delivery, with Reservoir on the client, and with Domain Modeling and Tributary for projection and domain registration surfaces.

## Use This Section

Start here when you need to understand generated projection, API, and registration alignment across the full Mississippi stack.

## Current Coverage

This section now includes typed boundary pages for getting started, concepts, package selection, reference, operations, and troubleshooting.

They establish the composition and source-generation boundary while deeper generated-surface documentation is still being written.

## Learn More

- [Documentation Home](../index.md) - Return to the product-area docs landing page
- [Inlet Getting Started](./getting-started/getting-started.md) - Start with the composition-layer entry points
- [Inlet Concepts](./concepts/concepts.md) - Understand how Inlet aligns client, gateway, and runtime surfaces
- [Inlet Reference](./reference/reference.md) - Review the currently verified package and ownership surface
- [Inlet Operations](./operations/operations.md) - See the current operational scope and open evidence gaps
- [Aqueduct](../aqueduct/index.md) - See the backplane used for real-time delivery
- [Archived Reference](../archived/reference/index.md) - Browse preserved reference material related to generated registrations
