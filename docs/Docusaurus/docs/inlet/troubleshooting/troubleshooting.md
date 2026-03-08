---
title: Troubleshoot Inlet Scope And Entry-Point Confusion
sidebar_position: 1
description: Resolve the common problem of starting in Inlet when the question is really confined to one Mississippi subsystem.
---

# Troubleshoot Inlet Scope And Entry-Point Confusion

## Symptom

You started in Inlet, but the question now appears to be only about one subsystem rather than cross-layer alignment.

## What This Usually Means

Inlet is the wrong primary entry point when the issue does not actually span client, gateway, and runtime surfaces together.

## Probable Causes

- The task belongs to [Aqueduct](../../aqueduct/index.md) because it is really only about the backplane.
- The task belongs to [Reservoir](../../reservoir/index.md) because it is really only about client state.
- The task belongs to [Domain Modeling](../../domain-modeling/index.md) because it is really only about domain behavior.

## How To Confirm

- Stay in Inlet only if the primary concern is generated or runtime-aligned behavior across multiple layers.

## Resolution

Use [How To Choose An Inlet Package Entry Point](../how-to/how-to.md) if the issue still belongs to Inlet. Otherwise switch to the correct subsystem overview.

## Verify The Fix

You should be able to describe the issue as cross-layer alignment or as a single-subsystem concern.

## Prevention

Start by asking whether the problem spans client, gateway, and runtime together or only one layer.

## Summary

Inlet is the correct section only when the composition problem crosses subsystem boundaries.

## Next Steps

- Read [Inlet Getting Started](../getting-started/getting-started.md).
- Move to the specific subsystem overview if the issue is confined to one layer.
