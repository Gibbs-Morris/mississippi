---
id: domain-modeling-how-to
title: How To Choose A Domain Modeling Package Entry Point
sidebar_label: How To
sidebar_position: 1
description: Select the correct Domain Modeling package based on whether you need contracts, runtime support, gateway support, or testing support.
---

# How To Choose A Domain Modeling Package Entry Point

## When To Use This Page

Use this page when you know the problem belongs to Domain Modeling but you still need the correct package boundary.

## Before You Begin

- Read the [Domain Modeling overview](../index.md).
- Confirm that the issue is really about business behavior rather than a lower-level stream or reduction concern.

## Steps

1. Choose `Mississippi.DomainModeling.Abstractions` for the domain-facing contracts.
2. Choose `Mississippi.DomainModeling.Runtime` for aggregate, saga, effect, and projection runtime support.
3. Choose `Mississippi.DomainModeling.Gateway` for gateway-facing concerns in this area.
4. Choose `Mississippi.DomainModeling.TestHarness` for testing support.
5. Switch to [Tributary](../../tributary/index.md) or [Brooks](../../brooks/index.md) if the real problem is lower in the stack.

## Verify The Result

- You should be able to explain whether the task is about contracts, runtime behavior, gateway support, or test support.

## Summary

Choose Domain Modeling packages by the domain-facing responsibility that owns the work.

## Next Steps

- Use [Domain Modeling Reference](../reference/reference.md) for the currently verified surface.
- Use [Domain Modeling Troubleshooting](../troubleshooting/troubleshooting.md) if the boundary still is not clear.
