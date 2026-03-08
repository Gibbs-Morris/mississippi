---
title: Aqueduct Operations
sidebar_position: 1
description: Current operational scope for Aqueduct and the evidence gaps that still need dedicated runtime guidance.
---

# Aqueduct Operations

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Operational Goal

The operational concern for Aqueduct is keeping the Orleans-backed SignalR backplane correctly placed between gateway and runtime hosts.

## When This Matters

Use this page when your question is operational rather than conceptual, but you need to stay within currently verified documentation.

## Prerequisites And Assumptions

- You already understand the [Aqueduct overview](../index.md).
- You know whether you are looking at gateway-side hosting, runtime-side hosting, or a higher-level Inlet scenario.

## Current Verified Operational Scope

The active docs currently verify that Aqueduct owns gateway-side hub lifetime management, notifier registration, and runtime-side backplane registration.

## What Is Not Yet Published

The active docs do not yet publish verified guidance for deployment topologies, configuration defaults, telemetry baselines, or incident response for Aqueduct.

## Validation

For now, validate your understanding by confirming that the problem is truly about the backplane boundary and not about projection generation, domain behavior, or client state.

## Failure Modes And Rollback

Detailed failure modes and rollback guidance remain unverified for publication and still need dedicated operational documentation.

## Telemetry To Watch

Specific telemetry guidance is not published yet in the active docs set.

## Summary

Use this page as the operational boundary marker for Aqueduct until detailed runtime guidance is published.

## Next Steps

- Use [Aqueduct Reference](../reference/reference.md) for the currently verified package surface.
- Use [Archived Documentation](../../archived/index.md) if you need preserved material while the active operations story is rebuilt.