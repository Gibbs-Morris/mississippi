---
id: domain-modeling-troubleshooting
title: Troubleshoot Domain Modeling Scope And Entry-Point Confusion
sidebar_label: Troubleshooting
sidebar_position: 1
description: Resolve the common problem of starting in Domain Modeling when the question is really about a lower-level subsystem.
---

# Troubleshoot Domain Modeling Scope And Entry-Point Confusion

## Symptoms

You started in Domain Modeling, but the question now appears to be about event streams, reducers, or another lower-level subsystem.

## What This Usually Means

Domain Modeling is the wrong primary entry point when the issue is not actually the domain-facing behavior layer.

## Probable Causes

- The task belongs to [Tributary](../../tributary/index.md) because it is really about reduction or snapshots.
- The task belongs to [Brooks](../../brooks/index.md) because it is really about raw streams or provider seams.
- The task belongs to [Inlet](../../inlet/index.md) because it is really about generated cross-layer alignment.

## How To Confirm

- Stay in Domain Modeling only if the primary concern is aggregate behavior, saga orchestration, effects, or UX projections.

## Resolution

Use [How To Choose A Domain Modeling Package Entry Point](../how-to/how-to.md) if the issue still belongs to Domain Modeling. Otherwise switch to the correct subsystem overview.

## Verify The Fix

You should be able to describe the issue as domain behavior, reduction layer, stream substrate, or generated alignment.

## Prevention

Start by asking whether the task is business behavior or enabling infrastructure.

## Summary

Domain Modeling is the correct section only when the business-facing behavior layer is the core problem.

## Next Steps

- Read [Domain Modeling Getting Started](../getting-started/getting-started.md).
- Move to [Tributary](../../tributary/index.md) or [Brooks](../../brooks/index.md) if the issue is lower in the stack.
