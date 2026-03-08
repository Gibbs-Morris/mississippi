---
id: brooks-getting-started
title: Brooks Getting Started
sidebar_label: Getting Started
sidebar_position: 1
description: Start with Brooks by confirming that your question is about event streams, serialization seams, or storage providers.
---

# Brooks Getting Started

## Outcome

Use this page to confirm whether Brooks is the correct subsystem and which package boundary to inspect first.

## What You Will Achieve

By the end of this page, you should know whether your question belongs to event streams, serialization seams, storage-provider boundaries, or a higher-level layer built above Brooks.

## Before You Begin

- Read the [Brooks overview](../index.md).
- Read [Tributary](../../tributary/index.md) if the question may actually be about reducers or snapshots.

## First Verified Success

1. Read the [Brooks overview](../index.md) and confirm the problem is about event streams, serializer seams, or provider boundaries.
2. Open [Brooks Reference](../reference/reference.md) and identify the package boundary that matches the work.
3. If the real question is about derived state or domain behavior, switch to [Tributary](../../tributary/index.md) or [Domain Modeling](../../domain-modeling/index.md).

## Choose Your Starting Point

- Start with `Mississippi.Brooks.Abstractions` for the event-stream contracts.
- Start with `Mississippi.Brooks.Runtime` for the runtime stream layer.
- Start with `Mississippi.Brooks.Serialization.Abstractions` for serialization seams.
- Start with `Mississippi.Brooks.Serialization.Json` when the question is about the JSON serializer implementation.

## Verify You Are In The Right Section

- Stay in Brooks when the concern is the stream substrate or a provider seam.
- Move to [Tributary](../../tributary/index.md) when the concern is state reduction or snapshots.
- Move to [Domain Modeling](../../domain-modeling/index.md) when the concern is domain behavior.

## Verify The Result

- You should be able to state whether the issue is about streams, serialization, a provider seam, or a higher-level subsystem.

## What This Page Does Not Yet Provide

This page does not yet publish a runnable provider setup walkthrough. That material still needs to be verified and written.

## Summary

Brooks is the right entry point when the problem is the event-stream foundation itself.

## Next Steps

- Read [Brooks Concepts](../concepts/concepts.md).
- Use [Brooks Reference](../reference/reference.md) for the currently verified package surface.
