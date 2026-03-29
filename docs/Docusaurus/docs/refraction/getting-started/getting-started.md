---
id: refraction-getting-started
title: Refraction Getting Started
sidebar_label: Getting Started
sidebar_position: 1
description: Start with Refraction by confirming that your question is about Blazor UX composition rather than state management.
---

# Refraction Getting Started

## Outcome

Use this page to confirm whether Refraction is the right place to start, which package boundary matches your UI question, and how to reach the first runnable Refraction success path.

## What You Will Achieve

By the end of this page, you should know whether your task is about state-down, events-up Blazor components, Reservoir-style client state, or another Mississippi layer, and you should have a verified path to run the LightSpeed base-only sample.

## Before You Begin

- Read the [Refraction overview](../index.md).
- If the task involves stores, reducers, or effects, also read [Reservoir](../../reservoir/index.md).

## First Verified Success

1. Read the [Refraction overview](../index.md) and confirm the problem is about the UI interaction contract rather than the state-management model.
2. From the repository root, run the LightSpeed sample:

    ```powershell
    dotnet run --project samples/LightSpeed/LightSpeed.AppHost/LightSpeed.AppHost.csproj
    ```

3. Open the LightSpeed home route and verify that you can:
    - switch between the `horizon` and `signal` brands,
    - search and filter the review queue,
    - select a work item,
    - open the review dialog,
    - validate and save edits, and
    - apply the next action.
4. Open [Refraction Reference](../reference/reference.md) and identify which Refraction package boundary matches the question you are trying to answer.
5. If the core issue is really store behavior, switch to [Reservoir](../../reservoir/index.md) before going further.

## Choose Your Starting Point

- Start with `Mississippi.Refraction.Abstractions` when you need the contracts behind the UI layer.
- Start with `Mississippi.Refraction.Client` when you need the Blazor component layer itself.
- Start with `Mississippi.Refraction.Client.StateManagement` only when the question is about Refraction's composition helpers around client state.

## Verify You Are In The Right Section

- Stay in Refraction when the concern is component interaction contracts and UI composition.
- Move to [Reservoir](../../reservoir/index.md) when the concern is the store, reducers, effects, or selectors.

## Verify The Result

- You should be able to state whether the issue is UI contract first or state-management first.
- You should have a runnable proof that `Mississippi.Refraction.Client` can deliver a branded, presentational workflow without Reservoir.

## Current Scope

This page covers package selection, subsystem orientation, and the first runnable base-only Refraction proof path in `samples/LightSpeed/`.

## Summary

Refraction is the right entry point when the problem is the Blazor UX contract itself: state down, events up. LightSpeed is the fastest way to prove that path before adding optional client-state layers.

## Next Steps

- Read [Refraction Concepts](../concepts/concepts.md).
- Use [Refraction Reference](../reference/reference.md) for the currently verified package surface.
