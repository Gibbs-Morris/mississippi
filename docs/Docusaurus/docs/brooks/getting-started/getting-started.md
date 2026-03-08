---
title: Brooks Getting Started
sidebar_position: 1
description: Start with Brooks by confirming that your question is about event streams, serialization seams, or storage providers.
---

# Brooks Getting Started

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Outcome

Use this page to confirm whether Brooks is the correct subsystem and which package boundary to inspect first.

## What You Will Achieve

By the end of this page, you should know whether your question belongs to event streams, serialization seams, storage-provider boundaries, or a higher-level layer built above Brooks.

## Before You Begin

- Read the [Brooks overview](../index.md).
- Read [Tributary](../../tributary/index.md) if the question may actually be about reducers or snapshots.

## Choose Your Starting Point

- Start with `Mississippi.Brooks.Abstractions` for the event-stream contracts.
- Start with `Mississippi.Brooks.Runtime` for the runtime stream layer.
- Start with `Mississippi.Brooks.Serialization.Abstractions` for serialization seams.
- Start with `Mississippi.Brooks.Serialization.Json` when the question is about the JSON serializer implementation.

## Verify You Are In The Right Section

- Stay in Brooks when the concern is the stream substrate or a provider seam.
- Move to [Tributary](../../tributary/index.md) when the concern is state reduction or snapshots.
- Move to [Domain Modeling](../../domain-modeling/index.md) when the concern is domain behavior.

## What This Page Does Not Yet Provide

This page does not yet publish a runnable provider setup walkthrough. That material still needs to be verified and written.

## Summary

Brooks is the right entry point when the problem is the event-stream foundation itself.

## Next Steps

- Read [Brooks Concepts](../concepts/concepts.md).
- Use [Brooks Reference](../reference/reference.md) for the currently verified package surface.