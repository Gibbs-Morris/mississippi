---
id: aqueduct-overview
title: Aqueduct
sidebar_label: Overview
sidebar_position: 1
description: Aqueduct provides the Orleans-backed SignalR backplane used for distributed real-time messaging across Mississippi hosts.
---

# Aqueduct

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
- Runtime-side backplane registration for silo hosts
- Aqueduct-specific options and abstractions for distributed message routing

## How It Fits Mississippi

Aqueduct can be used independently of the event-sourcing and source-generation layers.

Within the full Mississippi stack, Inlet uses Aqueduct for real-time projection delivery over SignalR.

## Use This Section

Start here when you need to understand Mississippi's real-time delivery layer, the backplane boundary, or the packages that wire SignalR across gateway and runtime hosts.

## Current Coverage

This section now includes typed boundary pages for getting started, concepts, package selection, reference, operations, and troubleshooting.

They establish the correct reading paths and subsystem boundaries without inventing unverified runtime detail.

## Learn More

- [Documentation Home](../index.md) - Return to the product-area docs landing page
- [Aqueduct Getting Started](./getting-started/getting-started.md) - Start with the package and subsystem entry points
- [Aqueduct Concepts](./concepts/concepts.md) - Understand the backplane boundary and how Aqueduct fits the stack
- [Aqueduct Reference](./reference/reference.md) - Review the currently verified package and ownership surface
- [Aqueduct Operations](./operations/operations.md) - See the current operational scope and open evidence gaps
- [Archived Documentation](../archived/index.md) - Browse the preserved pre-reset docs set
