---
id: aqueduct-operations
title: Aqueduct Operations
sidebar_label: Operations
sidebar_position: 1
description: Operate Aqueduct runtime composition with explicit provider, validation, and rollout boundaries.
---

# Aqueduct Operations

## Overview

This page describes the operational decisions around configuring Aqueduct in an Orleans runtime host. It focuses on the
provider boundary and composition-time validation; ordinary Orleans cluster operation remains host-specific.

## When this matters

Use this page when preparing a local or deployed Orleans host that will carry the Aqueduct backplane, or when a rollout
fails during composition.

## Prerequisites and assumptions

- The host has one `UseMississippi(...)` runtime terminal callback.
- The host either provides an Orleans stream provider or intentionally selects `UseMemoryStreams(...)` for local
  development or tests.
- When a gateway participates, verify that it uses the same provider and stream namespaces as the runtime.

## Recommended baseline

Use `runtime.AddAqueduct(...)` inside the terminal callback. Configure host-owned external providers before that
callback. For local development and tests, use `aqueduct.UseMemoryStreams()` so the final selected provider name is
used for both Aqueduct options and Orleans memory stream registration.

Do not make the runtime depend on an untracked provider name. Keep the selected `StreamProviderName`, server stream
namespace, and all-clients stream namespace consistent across every host that participates in the backplane.

## Operational guidance

Aqueduct validation runs during terminal composition. Empty or whitespace-only stream names reject the attachment with
structured diagnostics. A second `AddAqueduct(...)` call for the same runtime is also rejected.

`UseMemoryStreams(...)` is intended for development and tests. A deployed host should configure the stream provider it
needs through Orleans and set Aqueduct's `StreamProviderName` to that existing provider. The runtime builder does not
select or provision an external provider.

`UseMississippi(...)` stages service descriptors and applies queued native callbacks before publishing the runtime
graph. Composition does not start Orleans or check network reachability, so those checks belong to the host's normal
build and startup validation.

## Validation

After changing provider or namespace settings:

1. Run the host's normal build and composition checks.
2. Start the Orleans host with the intended provider configuration.
3. Confirm that the runtime and gateway resolve the same stream identities through their normal application checks.
4. Review the structured diagnostics if composition fails before startup.

## Failure modes and rollback

An invalid builder scope fails before its staged graph is attached. Correct the values and retry with a fresh
`UseMississippi(...)` composition. If publication itself damages the host service collection, the runtime composition
reference requires a fresh host; do not treat a partially restored host as a safe rollback target.

Changing stream identities can strand messages or split hosts across different backplane streams. Coordinate a provider
or namespace change as a deployment decision and verify every participating host before sending traffic.

## Telemetry to watch

Monitor the Orleans cluster health and stream-provider signals already exposed by the host. Aqueduct's composition
diagnostics are stable codes surfaced through `BuilderValidationException`; they are startup evidence rather than a
steady-state telemetry contract.

## Summary

Operate Aqueduct by keeping provider ownership explicit, composing it once through `RuntimeBuilder`, and validating the
host after startup. Use memory streams only for local development or tests unless a separate deployment decision has
established their suitability.

## Next Steps

- Use [Aqueduct Reference](../reference/reference.md) for options, defaults, and diagnostics.
- Read [Runtime Composition](../../reference/runtime-composition.md) for staging and failed-publication behavior.
- Follow [Aqueduct Troubleshooting](../troubleshooting/troubleshooting.md) when startup reports a composition or
  provider-resolution failure.
