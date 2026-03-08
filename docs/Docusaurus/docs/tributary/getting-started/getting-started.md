---
title: Tributary Getting Started
sidebar_position: 1
description: Start with Tributary by confirming that your question is about reducers or snapshots rather than raw streams or domain behavior.
---

# Tributary Getting Started

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Outcome

Use this page to confirm whether Tributary is the correct subsystem and which package boundary to inspect first.

## What You Will Achieve

By the end of this page, you should know whether your question belongs to reducers, snapshots, snapshot storage, or a neighboring layer above or below Tributary.

## Before You Begin

- Read the [Tributary overview](../index.md).
- Read [Brooks](../../brooks/index.md) if the question may still be about raw event streams.

## Choose Your Starting Point

- Start with `Mississippi.Tributary.Abstractions` for reducer and snapshot contracts.
- Start with `Mississippi.Tributary.Runtime` for runtime reduction and snapshot behavior.
- Start with `Mississippi.Tributary.Runtime.Storage.Abstractions` for snapshot storage seams.
- Start with `Mississippi.Tributary.Runtime.Storage.Cosmos` for the Cosmos-backed snapshot storage implementation.

## Verify You Are In The Right Section

- Stay in Tributary when the concern is derived state reconstruction or snapshots.
- Move to [Brooks](../../brooks/index.md) when the concern is raw streams.
- Move to [Domain Modeling](../../domain-modeling/index.md) when the concern is domain behavior.

## What This Page Does Not Yet Provide

This page does not yet publish a runnable reducer or snapshot quickstart. That material still needs to be verified and written.

## Summary

Tributary is the correct entry point when the problem is the reduction and snapshot layer.

## Next Steps

- Read [Tributary Concepts](../concepts/concepts.md).
- Use [Tributary Reference](../reference/reference.md) for the currently verified package surface.