---
id: reservoir-getting-started
title: Reservoir Getting Started
sidebar_label: Getting Started
sidebar_position: 1
description: Start with Reservoir by confirming that your question is about the client-state model rather than only UI composition.
---

# Reservoir Getting Started

## Outcome

Use this page to confirm whether Reservoir is the correct subsystem and which package boundary to inspect first.

## What You Will Achieve

By the end of this page, you should know whether your question is about stores, reducers, selectors, effects, or middleware and whether the active or archived Reservoir material is the better next stop.

## Before You Begin

- Read the [Reservoir overview](../index.md).
- Read [Refraction](../../refraction/index.md) if the UI layer boundary is still unclear.

## First Verified Success

1. Read the [Reservoir overview](../index.md) and confirm the question is about state transitions, reducers, selectors, effects, middleware, or testing.
2. Open [Reservoir Reference](../reference/reference.md) and identify the package boundary that matches the work.
3. If you need deeper preserved material immediately, jump to [Archived Reservoir Docs](../../archived/client-state-management/reservoir.md).

## Choose Your Starting Point

- Start with `Mississippi.Reservoir.Abstractions` when you need the core state-management contracts.
- Start with `Mississippi.Reservoir.Core` when the question is about the store, actions, reducers, selectors, effects, or middleware.
- Start with `Mississippi.Reservoir.Client` when the question is about client integration.
- Start with `Mississippi.Reservoir.TestHarness` when the question is about testing Reservoir-based code.

## Verify You Are In The Right Section

- Stay in Reservoir when the concern is the state-management model itself.
- Move to [Refraction](../../refraction/index.md) when the concern is primarily the Blazor UX contract.

## Verify The Result

- You should be able to identify the correct Reservoir package or conclude that the issue belongs in Refraction instead.

## Current Scope

This page covers package selection and subsystem orientation. For detailed Reservoir guidance including testing and API reference, see the [archived Reservoir material](../../archived/client-state-management/reservoir.md).

## Summary

Reservoir is the correct entry point when the problem is the client-state model rather than the UI component contract.

## Next Steps

- Read [Reservoir Concepts](../concepts/concepts.md).
- Use [Archived Reservoir Docs](../../archived/client-state-management/reservoir.md) for preserved deep material.
