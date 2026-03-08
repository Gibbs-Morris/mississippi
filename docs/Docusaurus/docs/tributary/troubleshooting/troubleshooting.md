---
id: tributary-troubleshooting
title: Troubleshoot Tributary Scope And Entry-Point Confusion
sidebar_label: Troubleshooting
sidebar_position: 1
description: Resolve the common problem of starting in Tributary when the question is really about raw streams or domain behavior.
---

# Troubleshoot Tributary Scope And Entry-Point Confusion

## Symptoms

You started in Tributary, but the question now appears to be about raw event streams or domain behavior.

## What This Usually Means

Tributary is the wrong primary entry point when the issue is not actually the reduction and snapshot layer.

## Probable Causes

- The task belongs to [Brooks](../../brooks/index.md) because it is really about event streams or serializer seams.
- The task belongs to [Domain Modeling](../../domain-modeling/index.md) because it is really about domain behavior.

## How To Confirm

- Stay in Tributary only if the primary concern is reducers, snapshots, or derived state reconstruction.

## Resolution

Use [How To Choose A Tributary Package Entry Point](../how-to/how-to.md) if the issue still belongs to Tributary. Otherwise switch to the correct subsystem overview.

## Verify The Fix

You should be able to describe the issue as stream substrate, reduction layer, or domain behavior.

## Prevention

Start by deciding whether the problem is below the reduction layer, inside it, or above it.

## Summary

Tributary is the correct section only when the reduction and snapshot layer is the core problem.

## Next Steps

- Read [Tributary Getting Started](../getting-started/getting-started.md).
- Move to [Brooks](../../brooks/index.md) if the issue is really about streams.
