---
id: aqueduct-troubleshooting
title: Troubleshoot Aqueduct Runtime Composition
sidebar_label: Troubleshooting
sidebar_position: 1
description: Diagnose Aqueduct runtime composition failures using stable diagnostics and provider checks.
---

# Troubleshoot Aqueduct Runtime Composition

Use this guide when an Orleans host fails while composing Aqueduct or when the selected stream provider cannot be
resolved after startup.

## Symptoms

- `UseMississippi(...)` throws `BuilderValidationException` while an `AddAqueduct(...)` callback is running.
- One of the Aqueduct diagnostics `MSB201`–`MSB203`, `MSB206`, or `MSB207` appears in the exception.
- The host starts composition but later cannot resolve the selected Orleans stream provider.

## What this usually means

The nested Aqueduct builder validates its own settings before the runtime graph is attached. Provider resolution is a
separate host concern that occurs after composition and depends on the host's Orleans stream setup.

## Probable causes

- A stream provider name or stream namespace is empty or whitespace-only (`MSB201`–`MSB203`).
- The same runtime received more than one `AddAqueduct(...)` call (`MSB207`).
- A captured `AqueductBuilder` was changed after its callback completed (`MSB206`).
- `StreamProviderName` does not match a provider configured by the Orleans host.
- A memory-stream setup was expected, but `UseMemoryStreams(...)` was not called in the runtime callback.

## How to confirm

1. Read `BuilderValidationException.Diagnostics` and record each `Code`, `Message`, and `Remediation`.
2. For `MSB201`–`MSB203`, inspect the values supplied to the one `AddAqueduct(...)` callback.
3. For `MSB206`, find the captured builder and move its property assignments into a fresh callback.
4. For `MSB207`, combine all Aqueduct settings into one call for the runtime.
5. For provider failures, compare the final `StreamProviderName` with the host's Orleans provider registration.
6. If the task is actually projection delivery, domain behavior, or client composition, switch to [Inlet](../../inlet/index.md),
   [Domain Modeling](../../domain-modeling/index.md), or [Reservoir](../../reservoir/index.md).

## Resolution

Correct the values and retry the complete runtime composition through a fresh `UseMississippi(...)` callback. For local
development or tests, use `aqueduct.UseMemoryStreams()` or `aqueduct.UseMemoryStreams("ProviderName")`. For a deployed
host, configure the external provider through Orleans and select that existing name in `AqueductBuilder`.

## Verify the fix

Build and start the host using its normal Orleans checks. Confirm that every participating host selects the intended
provider and stream namespaces. A successful composition alone does not prove network connectivity or a running Orleans
cluster.

## Prevention

Keep one `AddAqueduct(...)` call per runtime, configure values inside the terminal callback, and use the named
`AqueductBuilderDiagnosticCodes` constants when handling diagnostics programmatically. Keep host-owned provider names
in one configuration source so runtime and gateway settings can be reviewed together.

## Summary

Aqueduct composition failures are either nested-builder validation errors or host provider configuration errors. Use the
stable code first, then correct the callback or host provider and verify the full host startup.

## Next Steps

- Read [Aqueduct Reference](../reference/reference.md) for the complete option and diagnostic tables.
- Follow [How To Configure Aqueduct Runtime Composition](../how-to/how-to.md) for setup and migration.
- Read [Aqueduct Operations](../operations/operations.md) for rollout and provider guidance.
