---
id: refraction-overview
title: Refraction
sidebar_label: Overview
sidebar_position: 1
description: Refraction is a Blazor UX component library built around a state-down, events-up interaction model.
---

# Refraction

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Overview

Refraction is the Mississippi UX component library for Blazor applications.

Its defining model is state down and events up: components receive state through parameters and report user intent through events so application logic remains outside the component and composes cleanly with external state management.

## Why This Area Exists

Use Refraction when you want a UI layer that keeps rendering concerns and interaction contracts separate from application state and domain behavior.

It is meant to make Blazor component composition cleaner, more reusable, and easier to pair with external state-management approaches.

## Representative Packages

- `Mississippi.Refraction.Abstractions`
- `Mississippi.Refraction.Client`
- `Mississippi.Refraction.Client.StateManagement`

## What This Area Owns

- Reusable Blazor UI components
- Contracts and conventions for state-down, events-up component design
- Composition helpers for applications that want to build on the Refraction interaction model

## How It Fits Mississippi

Refraction is a UI-layer concern and can be used independently.

It is not the state-management layer. Reservoir is the Redux-style state-management subsystem that can sit underneath Refraction-based applications when that composition is useful.

## Use This Section

Start here when you need to understand the Mississippi UI layer, the state-down and events-up contract, or how Refraction relates to Reservoir in client applications.

## Current Coverage

This section now includes typed holding pages for getting started, concepts, package selection, reference, and troubleshooting.

They make the Refraction boundary explicit while deeper component and pattern documentation is still being written.

## Learn More

- [Documentation Home](../index.md) - Return to the product-area docs landing page
- [Refraction Getting Started](./getting-started/getting-started.md) - Start with the UX-layer entry points
- [Refraction Concepts](./concepts/concepts.md) - Understand the state-down, events-up model
- [Refraction Reference](./reference/reference.md) - Review the currently verified package and ownership surface
- [Reservoir](../reservoir/index.md) - See the client state-management layer that often composes with Refraction
- [Archived Documentation](../archived/index.md) - Browse the preserved pre-reset docs set
