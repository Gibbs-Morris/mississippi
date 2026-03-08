---
id: domain-modeling-overview
title: Domain Modeling
sidebar_label: Overview
sidebar_position: 1
description: Domain Modeling provides the aggregate, saga, and UX projection surfaces used to express Mississippi domain behavior.
---

# Domain Modeling

## Overview

Domain Modeling is the Mississippi area where aggregates, sagas, event effects, and UX projections are expressed.

It defines the domain-facing abstractions and runtime pieces that let application code focus on business rules instead of infrastructure plumbing.

## Why This Area Exists

Use Domain Modeling when you want Mississippi's domain-facing building blocks rather than the lower-level stream, reducer, or transport subsystems.

This is where application code expresses commands, aggregate behavior, sagas, effects, and UX projections in business terms.

## Representative Packages

- `Mississippi.DomainModeling.Abstractions`
- `Mississippi.DomainModeling.Runtime`
- `Mississippi.DomainModeling.Gateway`
- `Mississippi.DomainModeling.TestHarness`

## What This Area Owns

- Aggregate command-handling abstractions and runtime support
- Saga orchestration and compensation surfaces
- Event-effect patterns attached to domain behavior
- UX projection grain abstractions and runtime support

## How It Fits Mississippi

Domain Modeling sits above Brooks and Tributary.

It is the place where event streams and state reconstruction are turned into concrete domain behavior and projection-facing models.

## Use This Section

Start here when the question is about aggregates, sagas, effects, or UX projections rather than about the lower-level mechanics underneath them.

## Current Coverage

This section now includes typed boundary pages for getting started, concepts, package selection, reference, and troubleshooting.

They make the domain-facing boundary easier to navigate while deeper aggregate, saga, effect, and UX projection pages are still being written.

## Learn More

- [Documentation Home](../index.md) - Return to the product-area docs landing page
- [Domain Modeling Getting Started](./getting-started/getting-started.md) - Start with the domain-facing entry points
- [Domain Modeling Concepts](./concepts/concepts.md) - Understand the aggregate, saga, and projection boundary
- [Domain Modeling Reference](./reference/reference.md) - Review the currently verified package and ownership surface
- [Tributary](../tributary/index.md) - See the reducers and snapshots layer beneath domain behavior
- [Archived Concepts](../archived/concepts/index.md) - Browse the preserved concept material
