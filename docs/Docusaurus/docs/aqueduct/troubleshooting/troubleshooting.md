---
title: Troubleshoot Aqueduct Scope And Entry-Point Confusion
sidebar_position: 1
description: Resolve the common problem of starting in Aqueduct when the question belongs to another Mississippi area.
---

# Troubleshoot Aqueduct Scope And Entry-Point Confusion

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Symptom

You started in Aqueduct, but the question now seems to involve projections, domain behavior, or client concerns.

## What This Usually Means

Aqueduct is the wrong entry point when the real question is above the backplane layer.

## Probable Causes

- The task is actually about [Inlet](../../inlet/index.md) and full-stack projection delivery.
- The task is actually about [Domain Modeling](../../domain-modeling/index.md) and domain behavior.
- The task is actually about client state or UI composition, which belongs in [Reservoir](../../reservoir/index.md) or [Refraction](../../refraction/index.md).

## How To Confirm

- Stay in Aqueduct only if the concern is Orleans-backed SignalR backplane infrastructure.
- Move sections if the concern is generated surfaces, domain behavior, or client composition.

## Resolution

Use [How To Choose An Aqueduct Package Entry Point](../how-to/how-to.md) if the problem still belongs to Aqueduct. Otherwise switch to the correct subsystem overview and continue there.

## Verify The Fix

You should be able to describe the problem in one sentence using either backplane infrastructure, generated composition, domain behavior, or client state and UI as the primary concern.

## Prevention

Start each investigation from the subsystem boundary first, not from a package name alone.

## Summary

Aqueduct is only the right section when the problem is the distributed real-time backplane itself.

## Next Steps

- Read [Aqueduct Getting Started](../getting-started/getting-started.md).
- Move to [Inlet](../../inlet/index.md) if your question is really end-to-end projection delivery.