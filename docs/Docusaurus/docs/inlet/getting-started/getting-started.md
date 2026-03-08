---
title: Inlet Getting Started
sidebar_position: 1
description: Start with Inlet by confirming that your question is about generated cross-layer alignment rather than one subsystem in isolation.
---

# Inlet Getting Started

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Outcome

Use this page to confirm whether Inlet is the correct subsystem and which package boundary to inspect first.

## What You Will Achieve

By the end of this page, you should know whether your question belongs to generated client, gateway, and runtime alignment or whether it really belongs to Aqueduct, Reservoir, or Domain Modeling directly.

## Before You Begin

- Read the [Inlet overview](../index.md).
- Read [Aqueduct](../../aqueduct/index.md) if the question may actually be only about the backplane.

## Choose Your Starting Point

- Start with `Mississippi.Inlet.Abstractions` for shared projection-path and metadata contracts.
- Start with `Mississippi.Inlet.Client` when the concern is generated client-side projection support.
- Start with `Mississippi.Inlet.Gateway` when the concern is generated gateway APIs or SignalR delivery support.
- Start with `Mississippi.Inlet.Runtime` when the concern is runtime registration and generated silo wiring.

## Verify You Are In The Right Section

- Stay in Inlet when the concern is cross-layer alignment driven by source generation and runtime wiring.
- Move to [Aqueduct](../../aqueduct/index.md), [Reservoir](../../reservoir/index.md), or [Domain Modeling](../../domain-modeling/index.md) when the issue is isolated to one of those layers.

## What This Page Does Not Yet Provide

This page does not yet publish a runnable end-to-end generated walkthrough. Use the [Spring sample](../../samples/spring-sample/index.md) while that material is being rebuilt.

## Summary

Inlet is the correct entry point when the problem spans client, gateway, and runtime surfaces together.

## Next Steps

- Read [Inlet Concepts](../concepts/concepts.md).
- Use [Inlet Reference](../reference/reference.md) for the currently verified package surface.