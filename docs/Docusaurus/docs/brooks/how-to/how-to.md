---
id: brooks-how-to
title: How To Choose A Brooks Package Entry Point
sidebar_label: How To
sidebar_position: 1
description: Select the correct Brooks package based on whether you need stream contracts, runtime services, or serialization seams.
---

# How To Choose A Brooks Package Entry Point

## When To Use This Page

Use this page when you know the problem belongs to Brooks but you still need the first package boundary.

## Before You Begin

- Read the [Brooks overview](../index.md).
- Confirm that the question is about the stream substrate rather than a higher-level consumer.

## Steps

1. Choose `Mississippi.Brooks.Abstractions` for the event-stream contracts.
2. Choose `Mississippi.Brooks.Runtime` for runtime stream services.
3. Choose `Mississippi.Brooks.Serialization.Abstractions` for serializer contracts.
4. Choose `Mississippi.Brooks.Serialization.Json` for the JSON serializer implementation.
5. Switch to [Tributary](../../tributary/index.md) or [Domain Modeling](../../domain-modeling/index.md) if the real issue is above the stream layer.

## Verify The Result

- You should be able to explain whether your task is about stream contracts, runtime stream services, or serializer boundaries.

## Summary

Choose Brooks packages by stream responsibility first, then move upward only when the problem is no longer the stream substrate.

## Next Steps

- Use [Brooks Reference](../reference/reference.md) for the currently verified surface.
- Use [Brooks Troubleshooting](../troubleshooting/troubleshooting.md) if the boundary still is not clear.
