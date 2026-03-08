---
title: Domain Modeling Getting Started
sidebar_position: 1
description: Start with Domain Modeling by confirming that your question is about aggregates, sagas, effects, or UX projections.
---

# Domain Modeling Getting Started

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Outcome

Use this page to confirm whether Domain Modeling is the correct subsystem and which package boundary to inspect first.

## What You Will Achieve

By the end of this page, you should know whether your question belongs to aggregates, sagas, effects, UX projections, or a lower-level subsystem beneath Domain Modeling.

## Before You Begin

- Read the [Domain Modeling overview](../index.md).
- Read [Tributary](../../tributary/index.md) if the question may actually be about reduction or snapshots.

## Choose Your Starting Point

- Start with `Mississippi.DomainModeling.Abstractions` for the domain-facing contracts.
- Start with `Mississippi.DomainModeling.Runtime` for aggregate, saga, effect, and projection runtime support.
- Start with `Mississippi.DomainModeling.Gateway` when the concern is the gateway-facing surface in this area.
- Start with `Mississippi.DomainModeling.TestHarness` when the concern is testing support.

## Verify You Are In The Right Section

- Stay in Domain Modeling when the concern is business behavior or UX projection behavior.
- Move to [Tributary](../../tributary/index.md) when the concern is reduction and snapshots.
- Move to [Brooks](../../brooks/index.md) when the concern is raw event streams.

## What This Page Does Not Yet Provide

This page does not yet publish a runnable aggregate or saga quickstart. That material still needs to be verified and written.

## Summary

Domain Modeling is the correct entry point when the problem is the domain-facing behavior layer.

## Next Steps

- Read [Domain Modeling Concepts](../concepts/concepts.md).
- Use [Domain Modeling Reference](../reference/reference.md) for the currently verified package surface.