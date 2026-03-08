---
title: How To Choose An Aqueduct Package Entry Point
sidebar_position: 1
description: Select the correct Aqueduct package based on whether you need contracts, gateway hosting, or runtime hosting.
---

# How To Choose An Aqueduct Package Entry Point

## When To Use This Page

Use this page when you know the problem belongs to Aqueduct but you still need to choose the first package or documentation path.

## Before You Begin

- Read the [Aqueduct overview](../index.md).
- Confirm that your problem is about the backplane rather than higher-level projection delivery.

## Steps

1. Choose `Mississippi.Aqueduct.Abstractions` if you need the contracts and options surface.
2. Choose `Mississippi.Aqueduct.Gateway` if the work is on the gateway side of SignalR integration.
3. Choose `Mississippi.Aqueduct.Runtime` if the work is on the Orleans runtime side of the backplane.
4. Switch to [Inlet](../../inlet/index.md) if the real question is projection subscriptions or generated end-to-end delivery.

## Verify The Result

- You should be able to explain why your task belongs to contracts, gateway hosting, runtime hosting, or a higher-level Inlet scenario.

## Summary

Pick Aqueduct packages by host boundary first, then move up to Inlet only when the problem extends beyond the backplane itself.

## Next Steps

- Use [Aqueduct Reference](../reference/reference.md) for the currently verified package surface.
- Use [Aqueduct Troubleshooting](../troubleshooting/troubleshooting.md) if you still are not sure this is the right section.
