---
id: inlet-operations
title: Inlet Operations
sidebar_label: Operations
sidebar_position: 1
description: Current operational scope for Inlet and the evidence gaps that still need dedicated generator and runtime guidance.
---

# Inlet Operations

## Operational Goal

The operational concern for Inlet is keeping generated client, gateway, and runtime surfaces aligned across the stack.

## When This Matters

Use this page when your question is operational and relates to generator output, deployment validation, or cross-layer alignment.

## Prerequisites And Assumptions

- You already understand the [Inlet overview](../index.md).
- You know whether the question is about client support, gateway support, runtime registration, or cross-layer alignment more broadly.

## Current Verified Operational Scope

The active docs currently verify that Inlet owns aligned client, gateway, and runtime surfaces plus the generators that connect them.

## Current Scope

This page covers the operational boundary for Inlet generator alignment across client, gateway, and runtime surfaces. For package-level details, see the [Inlet Reference](../reference/reference.md).

## Validation

Validate your next step by confirming that the problem truly spans multiple layers and is not only an [Aqueduct](../../aqueduct/index.md), [Reservoir](../../reservoir/index.md), or [Domain Modeling](../../domain-modeling/index.md) issue.

## Failure Modes And Rollback

Refer to the [Inlet Reference](../reference/reference.md) for failure behavior at the package level. Generator output misalignment typically surfaces at compile time rather than runtime.

## Telemetry To Watch

Monitor standard Orleans silo metrics and compilation diagnostics for any project using Inlet generators.

## Summary

This page establishes the operational boundary for Inlet generator and composition operations.

## Next Steps

- Use [Inlet Reference](../reference/reference.md) for the currently verified package surface.
- Use the [Spring Sample](../../samples/spring-sample/index.md) for a working example of Inlet composition in practice.
