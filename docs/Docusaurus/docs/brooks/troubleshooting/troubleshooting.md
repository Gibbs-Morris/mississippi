---
id: brooks-troubleshooting
title: Troubleshoot Brooks Scope And Entry-Point Confusion
sidebar_label: Troubleshooting
sidebar_position: 1
description: Resolve the common problem of starting in Brooks when the question is really about reducers, snapshots, or domain behavior.
---

# Troubleshoot Brooks Scope And Entry-Point Confusion

## Symptom

You started in Brooks, but the question now appears to be about derived state, aggregate behavior, or another layer above event streams.

## What This Usually Means

Brooks is the wrong primary entry point when the issue is not actually the event-stream substrate.

## Probable Causes

- The task belongs to [Tributary](../../tributary/index.md) because it is really about reducers or snapshots.
- The task belongs to [Domain Modeling](../../domain-modeling/index.md) because it is really about domain behavior.

## How To Confirm

- Stay in Brooks only if the concern is stream runtime behavior, serialization seams, or provider boundaries.

## Resolution

Use [How To Choose A Brooks Package Entry Point](../how-to/how-to.md) if the issue still belongs to Brooks. Otherwise switch to the correct subsystem overview.

## Verify The Fix

You should be able to describe the issue as stream substrate, derived state, or domain behavior.

## Prevention

Start by asking whether the problem is below or above the reduction and domain layers.

## Summary

Brooks is the correct section only when the event-stream substrate is the core problem.

## Next Steps

- Read [Brooks Getting Started](../getting-started/getting-started.md).
- Move to [Tributary](../../tributary/index.md) if the issue is really about reducers or snapshots.
