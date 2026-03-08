---
id: aqueduct-operations
title: Aqueduct Operations
sidebar_label: Operations
sidebar_position: 1
description: Current operational scope for Aqueduct and the evidence gaps that still need dedicated runtime guidance.
---

# Aqueduct Operations

## Operational Goal

The operational concern for Aqueduct is keeping the Orleans-backed SignalR backplane correctly placed between gateway and runtime hosts.

## When This Matters

Use this page when your question is operational rather than conceptual, but you need to stay within currently verified documentation.

## Prerequisites And Assumptions

- You already understand the [Aqueduct overview](../index.md).
- You know whether you are looking at gateway-side hosting, runtime-side hosting, or a higher-level Inlet scenario.

## Current Verified Operational Scope

The active docs currently verify that Aqueduct owns gateway-side hub lifetime management, notifier registration, and runtime-side backplane registration.

## Current Scope

This page covers the operational boundary for Aqueduct gateway and runtime hosting. For package-level details, see the [Aqueduct Reference](../reference/reference.md).

## Validation

For now, validate your understanding by confirming that the problem is truly about the backplane boundary and not about projection generation, domain behavior, or client state.

## Failure Modes And Rollback

Refer to the [Aqueduct Reference](../reference/reference.md) for failure behavior at the package level. Orleans cluster diagnostics apply to any silo hosting Aqueduct components.

## Telemetry To Watch

Monitor standard Orleans silo metrics and cluster health dashboards for any silo hosting Aqueduct backplane components.

## Summary

This page establishes the operational boundary for Aqueduct gateway and runtime hosting.

## Next Steps

- Use [Aqueduct Reference](../reference/reference.md) for the currently verified package surface.
- Use [Archived Documentation](../../archived/index.md) for additional preserved material on Aqueduct operations.
