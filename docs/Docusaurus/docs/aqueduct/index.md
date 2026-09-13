---
id: aqueduct-overview
title: SignalR Backplane (Aqueduct)
sidebar_label: Overview
sidebar_position: 1
description: Understand Aqueduct, Mississippi's Orleans-backed SignalR backplane for distributed real-time messaging.
---

# SignalR Backplane (Aqueduct)

## Overview

Aqueduct is the Mississippi area responsible for using Orleans as a SignalR backplane and push-delivery layer.

It lets Orleans-managed events and notifications be pushed through SignalR across servers while giving application code a way to send real-time updates without taking a direct dependency on SignalR infrastructure.

## Why This Area Exists

Use Aqueduct when you need distributed real-time delivery but do not want application code to own the mechanics of hub lifetime coordination, fan-out, and cross-node routing.

It gives Mississippi a dedicated backplane layer instead of forcing SignalR concerns into domain or client code.

## Representative Packages

- `Mississippi.Aqueduct.Abstractions`
- `Mississippi.Aqueduct.Gateway`
- `Mississippi.Aqueduct.Runtime`

## What This Area Owns

- SignalR backplane integration built on Orleans grains and streams
- Orleans-driven push delivery of events and notifications into SignalR-connected clients
- Gateway-side hub lifetime management and notifier registration
- Runtime-side backplane registration for silo hosts through `RuntimeBuilder`
- Aqueduct-specific options and abstractions for distributed message routing

## How It Fits Mississippi

Aqueduct can be used independently of the event-sourcing and source-generation layers.

Within the full Mississippi stack, Inlet uses Aqueduct for real-time projection delivery over SignalR.

An Orleans silo composes Aqueduct inside the canonical `UseMississippi(...)` terminal callback. The runtime
extension is `runtime.AddAqueduct(...)`; it is a nested scope on `RuntimeBuilder`, so the runtime host keeps
ownership of advanced service and native Orleans configuration.

## Use This Section

Start here when you need to understand Mississippi's real-time delivery layer, the backplane boundary, or the packages that wire SignalR across gateway and runtime hosts.

## Current Coverage

This section includes the runtime composition path, option reference, concepts, operations, and troubleshooting
guidance for the current Aqueduct surface. Gateway-side hub registration remains a separate host concern.

The pages distinguish verified runtime behavior from host configuration that remains application-owned.

## Learn More

- [Documentation Home](../index.md) - Return to the product-area docs landing page
- [Aqueduct Getting Started](./getting-started/getting-started.md) - Start with the package and subsystem entry points
- [Aqueduct Concepts](./concepts/concepts.md) - Understand the backplane boundary and how Aqueduct fits the stack
- [Aqueduct Reference](./reference/reference.md) - Look up `AddAqueduct`, options, defaults, and diagnostics
- [Aqueduct Runtime Migration](./migration/migration.md) - Move to the Next runtime composition contract
- [Aqueduct Operations](./operations/operations.md) - Configure and operate the runtime backplane safely
- [Archived Documentation](../archived/index.md) - Browse the preserved pre-reset docs set
