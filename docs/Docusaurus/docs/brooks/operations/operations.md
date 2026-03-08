---
id: brooks-operations
title: Brooks Operations
sidebar_label: Operations
sidebar_position: 1
description: Current operational scope for Brooks and the evidence gaps that still need dedicated provider guidance.
---

# Brooks Operations

## Operational Goal

The operational concern for Brooks is keeping the event-stream and provider boundary clear across runtime, serialization, and storage concerns.

## When This Matters

Use this page when your question is operational and relates to stream runtime, serialization, or provider boundary behavior.

## Prerequisites And Assumptions

- You already understand the [Brooks overview](../index.md).
- You know whether the question is about stream runtime behavior, serialization seams, or a provider boundary.

## Current Verified Operational Scope

The active docs currently verify that Brooks owns the runtime stream layer, serialization seams, and provider contracts.

## Current Scope

This page covers the operational boundary for Brooks stream runtime, serialization seams, and provider contracts. For package-level details, see the [Brooks Reference](../reference/reference.md).

## Validation

Validate your next step by confirming whether the issue is truly the stream substrate or whether it belongs in [Tributary](../../tributary/index.md) or [Domain Modeling](../../domain-modeling/index.md).

## Failure Modes And Rollback

Refer to the [Brooks Reference](../reference/reference.md) for failure behavior at the package level. Provider-specific diagnostics follow standard Orleans persistence and stream patterns.

## Telemetry To Watch

Monitor standard Orleans stream and persistence metrics for any silo hosting Brooks components.

## Summary

This page establishes the operational boundary for Brooks stream runtime and provider behavior.

## Next Steps

- Use [Brooks Reference](../reference/reference.md) for the currently verified package surface.
- Use [Archived Documentation](../../archived/index.md) for additional preserved material on Brooks operations.
