---
title: Inlet Operations
sidebar_position: 1
description: Current operational scope for Inlet and the evidence gaps that still need dedicated generator and runtime guidance.
---

# Inlet Operations

## Operational Goal

The operational concern for Inlet is keeping generated client, gateway, and runtime surfaces aligned across the stack.

## When This Matters

Use this page when your question is operational, but the active docs do not yet provide a fully rebuilt generator and runtime operations guide.

## Prerequisites And Assumptions

- You already understand the [Inlet overview](../index.md).
- You know whether the question is about client support, gateway support, runtime registration, or cross-layer alignment more broadly.

## Current Verified Operational Scope

The active docs currently verify that Inlet owns aligned client, gateway, and runtime surfaces plus the generators that connect them.

## What Is Not Yet Published

The active docs do not yet publish detailed operational guidance for generation workflows, deployment validation, telemetry, or incident response in Inlet-heavy systems.

## Validation

Validate your next step by confirming that the problem truly spans multiple layers and is not only an [Aqueduct](../../aqueduct/index.md), [Reservoir](../../reservoir/index.md), or [Domain Modeling](../../domain-modeling/index.md) issue.

## Failure Modes And Rollback

Detailed generator and runtime rollback guidance remains unverified for publication.

## Telemetry To Watch

Specific telemetry guidance is not published yet in the active Inlet section.

## Summary

Use this page as the operational boundary marker for Inlet until the detailed generator and composition operations story is rebuilt.

## Next Steps

- Use [Inlet Reference](../reference/reference.md) for the currently verified package surface.
- Use [Spring Sample](../../samples/spring-sample/index.md) for a verified sample entry point while active operations guidance is rebuilt.
