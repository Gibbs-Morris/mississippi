---
id: refraction-concepts
title: Refraction Concepts
sidebar_label: Concepts
sidebar_position: 1
description: Understand Refraction as Mississippi's state-down, events-up Blazor UX layer.
---

# Refraction Concepts

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

## What This Page Guarantees

- It defines Refraction as the Blazor UX layer organized around the state-down, events-up interaction model.
- It identifies Reservoir as the neighboring subsystem when the problem is really the state model rather than the UI contract.

## What This Page Does Not Claim

- A complete component catalog or API reference
- Rendering, performance, or lifecycle guarantees
- Full guidance for every supported application composition pattern

## Trade-Off To Keep In Mind

Refraction gives you a clear UI contract, but it does not replace a full state-management model when the application needs one.

## Summary

Think of Refraction as the UX layer that stays clean by pushing state and domain logic outside the component boundary.

## Related Reading

- [Refraction Getting Started](../getting-started/getting-started.md)
- [Reservoir Overview](../../reservoir/index.md)
- [Refraction Reference](../reference/reference.md)
