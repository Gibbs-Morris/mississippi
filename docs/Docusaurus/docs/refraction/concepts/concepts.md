---
title: Refraction Concepts
sidebar_position: 1
description: Understand Refraction as Mississippi's state-down, events-up Blazor UX layer.
---

# Refraction Concepts

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Problem This Area Solves

Refraction exists to keep Blazor rendering concerns and interaction contracts separate from application state and domain behavior.

## Core Idea

State flows into components through parameters, and user intent flows back out through events.

## How It Works

This model keeps components reusable and keeps application logic outside the component boundary.

## What This Area Owns

- Blazor UI components
- State-down, events-up interaction conventions
- Composition helpers for using that interaction model in applications

## What This Area Does Not Own

- The primary client-state store model
- Reducers, selectors, middleware, and effects as a subsystem

## Trade-Off To Keep In Mind

Refraction gives you a clear UI contract, but it does not replace a full state-management model when the application needs one.

## Summary

Think of Refraction as the UX layer that stays clean by pushing state and domain logic outside the component boundary.

## Related Reading

- [Refraction Getting Started](../getting-started/getting-started.md)
- [Reservoir Overview](../../reservoir/index.md)
- [Refraction Reference](../reference/reference.md)