---
title: Refraction Getting Started
sidebar_position: 1
description: Start with Refraction by confirming that your question is about Blazor UX composition rather than state management.
---

# Refraction Getting Started

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Outcome

Use this page to confirm whether Refraction is the right place to start and which package boundary matches your UI question.

## What You Will Achieve

By the end of this page, you should know whether your task is about state-down, events-up Blazor components, Reservoir-style client state, or another Mississippi layer.

## Before You Begin

- Read the [Refraction overview](../index.md).
- If the task involves stores, reducers, or effects, also read [Reservoir](../../reservoir/index.md).

## Choose Your Starting Point

- Start with `Mississippi.Refraction.Abstractions` when you need the contracts behind the UI layer.
- Start with `Mississippi.Refraction.Client` when you need the Blazor component layer itself.
- Start with `Mississippi.Refraction.Client.StateManagement` only when the question is about Refraction's composition helpers around client state.

## Verify You Are In The Right Section

- Stay in Refraction when the concern is component interaction contracts and UI composition.
- Move to [Reservoir](../../reservoir/index.md) when the concern is the store, reducers, effects, or selectors.

## What This Page Does Not Yet Provide

This page does not publish a verified runnable UI quickstart yet. That walkthrough still needs to be written.

## Summary

Refraction is the right entry point when the problem is the Blazor UX contract itself: state down, events up.

## Next Steps

- Read [Refraction Concepts](../concepts/concepts.md).
- Use [Refraction Reference](../reference/reference.md) for the currently verified package surface.