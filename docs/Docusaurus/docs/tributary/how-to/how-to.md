---
title: How To Choose A Tributary Package Entry Point
sidebar_position: 1
description: Select the correct Tributary package based on whether you need contracts, runtime reduction, or snapshot storage seams.
---

# How To Choose A Tributary Package Entry Point

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## When To Use This Page

Use this page when you know the problem belongs to Tributary but you still need the correct package boundary.

## Before You Begin

- Read the [Tributary overview](../index.md).
- Confirm that the question is about reducers or snapshots rather than raw streams or domain behavior.

## Steps

1. Choose `Mississippi.Tributary.Abstractions` for reducer and snapshot contracts.
2. Choose `Mississippi.Tributary.Runtime` for runtime reduction and snapshot behavior.
3. Choose `Mississippi.Tributary.Runtime.Storage.Abstractions` for snapshot storage seams.
4. Choose `Mississippi.Tributary.Runtime.Storage.Cosmos` for the Cosmos-backed snapshot storage implementation.
5. Switch to [Brooks](../../brooks/index.md) or [Domain Modeling](../../domain-modeling/index.md) if the real problem is outside the reduction layer.

## Verify The Result

- You should be able to explain whether the task is about contracts, runtime reduction, or snapshot storage seams.

## Summary

Choose Tributary packages by reduction-layer responsibility, then move up or down the stack only if the problem no longer belongs there.

## Next Steps

- Use [Tributary Reference](../reference/reference.md) for the currently verified surface.
- Use [Tributary Troubleshooting](../troubleshooting/troubleshooting.md) if the boundary still is not clear.