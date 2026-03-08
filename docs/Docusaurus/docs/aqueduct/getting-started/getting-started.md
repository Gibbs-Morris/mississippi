---
title: Aqueduct Getting Started
sidebar_position: 1
description: Start with Aqueduct by choosing the correct package boundary and neighboring Mississippi areas.
---

# Aqueduct Getting Started

:::warning Holding Page
This page is a holding page awaiting full content in a future pull request. It exists to establish the navigation path and currently verified subsystem boundary. Some details may still be incomplete or revised as the active documentation set is rebuilt.
:::

## Outcome

Use this page to identify whether Aqueduct is the right subsystem for your problem and which package boundary to inspect first.

## What You Will Achieve

By the end of this page, you should know whether your question is about the Aqueduct backplane itself, a higher-level Inlet scenario, or a different Mississippi layer entirely.

## Before You Begin

- Read the [Aqueduct overview](../index.md).
- If your question is about projection delivery rather than backplane infrastructure, also read [Inlet](../../inlet/index.md).

## Choose Your Starting Point

- Start with `Mississippi.Aqueduct.Abstractions` when you need contracts and options without runtime hosting concerns.
- Start with `Mississippi.Aqueduct.Gateway` when your question is about gateway-side hub lifetime management or notifier registration.
- Start with `Mississippi.Aqueduct.Runtime` when your question is about runtime-side backplane registration.

## Verify You Are In The Right Section

- Stay in Aqueduct when the concern is SignalR backplane behavior across hosts.
- Move to [Inlet](../../inlet/index.md) when the concern is full-stack projection delivery.
- Move to [Domain Modeling](../../domain-modeling/index.md) when the concern is domain behavior rather than transport.

## What This Page Does Not Yet Provide

This getting-started page does not publish a runnable end-to-end quickstart yet. A verified Aqueduct-focused walkthrough still needs to be written.

## Summary

Aqueduct is the right starting point when the problem is Orleans-backed SignalR backplane infrastructure, not domain behavior or higher-level generated delivery surfaces.

## Next Steps

- Read [Aqueduct Concepts](../concepts/concepts.md).
- Use [Aqueduct Reference](../reference/reference.md) for the currently verified package surface.
- Use [Aqueduct Operations](../operations/operations.md) if your next question is operational.