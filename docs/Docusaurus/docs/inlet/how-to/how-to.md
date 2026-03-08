---
id: inlet-how-to
title: How To Choose An Inlet Package Entry Point
sidebar_label: How To
sidebar_position: 1
description: Select the correct Inlet package based on whether you need shared abstractions, client support, gateway support, or runtime support.
---

# How To Choose An Inlet Package Entry Point

## When To Use This Page

Use this page when you know the problem belongs to Inlet but you still need the correct package boundary.

## Before You Begin

- Read the [Inlet overview](../index.md).
- Confirm that the question spans multiple layers rather than only one subsystem.

## Steps

1. Choose `Mississippi.Inlet.Abstractions` for shared projection-path and metadata contracts.
2. Choose `Mississippi.Inlet.Client` for generated client-side projection support.
3. Choose `Mississippi.Inlet.Gateway` for generated gateway APIs and SignalR delivery support.
4. Choose `Mississippi.Inlet.Runtime` for runtime registration and generated silo wiring.
5. Switch to [Aqueduct](../../aqueduct/index.md), [Reservoir](../../reservoir/index.md), or [Domain Modeling](../../domain-modeling/index.md) if the real issue is confined to one subsystem.

## Verify The Result

- You should be able to explain which cross-layer boundary owns the work and why.

## Summary

Choose Inlet packages by which aligned layer surface you are working on: shared contracts, client, gateway, or runtime.

## Next Steps

- Use [Inlet Reference](../reference/reference.md) for the currently verified surface.
- Use [Inlet Troubleshooting](../troubleshooting/troubleshooting.md) if the boundary still is not clear.
