---
id: inlet-getting-started
title: Inlet Getting Started
sidebar_label: Getting Started
sidebar_position: 1
description: Start with Inlet by confirming that your question is about generated cross-layer alignment rather than one subsystem in isolation.
---

# Inlet Getting Started

## Outcome

Use this page to confirm whether Inlet is the correct subsystem and which package boundary to inspect first.

## What You Will Achieve

By the end of this page, you should know whether your question belongs to generated client, gateway, and runtime alignment or whether it really belongs to Aqueduct, Reservoir, or Domain Modeling directly.

## Before You Begin

- Read the [Inlet overview](../index.md).
- Read [Aqueduct](../../aqueduct/index.md) if the question may actually be only about the backplane.

## First Verified Success

1. Read the [Inlet overview](../index.md) and confirm the problem spans client, gateway, and runtime surfaces together.
2. Open [Inlet Reference](../reference/reference.md) and identify which package boundary matches the work.
3. If you need a verified end-to-end path immediately, continue into the [Spring Sample](../../samples/spring-sample/index.md).

## Choose Your Starting Point

- Start with `Mississippi.Inlet.Abstractions` for shared projection-path and metadata contracts.
- Start with `Mississippi.Inlet.Client` when the concern is generated client-side projection support.
- Start with `Mississippi.Inlet.Gateway` when the concern is generated gateway APIs or SignalR delivery support.
- Start with `Mississippi.Inlet.Runtime` when the concern is runtime registration and generated silo wiring.

## Verify You Are In The Right Section

- Stay in Inlet when the concern is cross-layer alignment driven by source generation and runtime wiring.
- Move to [Aqueduct](../../aqueduct/index.md), [Reservoir](../../reservoir/index.md), or [Domain Modeling](../../domain-modeling/index.md) when the issue is isolated to one of those layers.

## Verify The Result

- You should be able to state whether the issue is truly cross-layer or whether it belongs to one subsystem in isolation.

## What This Page Does Not Yet Provide

This page does not yet publish a runnable end-to-end generated walkthrough. Use the [Spring sample](../../samples/spring-sample/index.md) while that material is being rebuilt.

## Summary

Inlet is the correct entry point when the problem spans client, gateway, and runtime surfaces together.

## Next Steps

- Read [Inlet Concepts](../concepts/concepts.md).
- Use [Inlet Reference](../reference/reference.md) for the currently verified package surface.
