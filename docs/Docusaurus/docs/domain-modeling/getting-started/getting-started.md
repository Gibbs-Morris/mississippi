---
id: domain-modeling-getting-started
title: Domain Modeling Getting Started
sidebar_label: Getting Started
sidebar_position: 1
description: Start with Domain Modeling by confirming that your question is about aggregates, sagas, effects, or UX projections.
---

# Domain Modeling Getting Started

## Outcome

Use this page to confirm whether Domain Modeling is the correct subsystem and which package boundary to inspect first.

## What You Will Achieve

By the end of this page, you should know whether your question belongs to aggregates, sagas, effects, UX projections, or a lower-level subsystem beneath Domain Modeling.

## Before You Begin

- Read the [Domain Modeling overview](../index.md).
- Read [Tributary](../../tributary/index.md) if the question may actually be about reduction or snapshots.

## First Verified Success

1. Read the [Domain Modeling overview](../index.md) and confirm the problem is about aggregates, sagas, effects, or UX projections.
2. Open [Domain Modeling Reference](../reference/reference.md) and identify the package boundary that matches the work.
3. If you need a verified end-to-end example immediately, continue into the [Spring Sample](../../samples/spring-sample/index.md).

## Choose Your Starting Point

- Start with `Mississippi.DomainModeling.Abstractions` for the domain-facing contracts.
- Start with `Mississippi.DomainModeling.Runtime` for aggregate, saga, effect, and projection runtime support.
- Start with `Mississippi.DomainModeling.Gateway` when the concern is the gateway-facing surface in this area.
- Start with `Mississippi.DomainModeling.TestHarness` when the concern is testing support.

## Verify You Are In The Right Section

- Stay in Domain Modeling when the concern is business behavior or UX projection behavior.
- Move to [Tributary](../../tributary/index.md) when the concern is reduction and snapshots.
- Move to [Brooks](../../brooks/index.md) when the concern is raw event streams.

## Verify The Result

- You should be able to explain whether the issue is domain-facing behavior or lower-level infrastructure.

## What This Page Does Not Yet Provide

This page does not yet publish a runnable aggregate or saga quickstart. That material still needs to be verified and written.

## Summary

Domain Modeling is the correct entry point when the problem is the domain-facing behavior layer.

## Next Steps

- Read [Domain Modeling Concepts](../concepts/concepts.md).
- Use [Domain Modeling Reference](../reference/reference.md) for the currently verified package surface.
