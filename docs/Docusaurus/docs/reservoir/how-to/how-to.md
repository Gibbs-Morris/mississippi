---
title: How To Choose A Reservoir Package Entry Point
sidebar_position: 1
description: Select the correct Reservoir package based on whether you need contracts, core state management, client integration, or testing support.
---

# How To Choose A Reservoir Package Entry Point

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## When To Use This Page

Use this page when you know the problem belongs to Reservoir but you still need the correct package boundary.

## Before You Begin

- Read the [Reservoir overview](../index.md).
- Confirm that the primary concern is state management, not only UI composition.

## Steps

1. Choose `Mississippi.Reservoir.Abstractions` for contracts.
2. Choose `Mississippi.Reservoir.Core` for the state-management model itself.
3. Choose `Mississippi.Reservoir.Client` for client integration concerns.
4. Choose `Mississippi.Reservoir.TestHarness` for testing support.
5. Switch to [Refraction](../../refraction/index.md) if the real issue is component interaction rather than state transitions.

## Verify The Result

- You should be able to explain which package boundary owns the work and why.

## Summary

Choose Reservoir packages by responsibility: contracts, core state model, client integration, or testing.

## Next Steps

- Use [Reservoir Reference](../reference/reference.md) for the currently verified surface.
- Use [Reservoir Troubleshooting](../troubleshooting/troubleshooting.md) if the boundary still is not clear.