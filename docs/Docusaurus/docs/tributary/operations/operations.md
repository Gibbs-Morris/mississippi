---
id: tributary-operations
title: Tributary Operations
sidebar_label: Operations
sidebar_position: 1
description: Current operational scope for Tributary and the evidence gaps that still need dedicated reducer and snapshot guidance.
---

# Tributary Operations

## Overview

Use this page when your question is operational and relates to reducer runtime behavior or snapshot persistence.

## Operational Goal

The operational concern for Tributary is keeping reducer and snapshot concerns separate from raw streams below and domain behavior above.

## Prerequisites And Assumptions

- You already understand the [Tributary overview](../index.md).
- You know whether the concern is runtime reduction, snapshot persistence, or a neighboring layer.

## Current Verified Operational Scope

The active docs currently verify that Tributary owns reducer runtime behavior, snapshot abstractions, and snapshot storage seams.

## Current Scope

This page covers the operational boundary for Tributary reducer runtime and snapshot storage. For package-level details, see the [Tributary Reference](../reference/reference.md).

## Validation

Validate the Blob provider rollout by confirming all of the following:

1. The keyed `BlobServiceClient` registration exists for the configured `BlobServiceClientServiceKey`.
2. The configured `PayloadSerializerFormat` matches exactly one registered serializer.
3. The configured container behavior matches the deployment model.
4. Stream-local maintenance cost is acceptable for the expected snapshots-per-stream distribution.
5. The issue is not really a [Brooks](../../brooks/index.md) stream problem or a [Domain Modeling](../../domain-modeling/index.md) behavior problem.

## Blob Snapshot Provider Guidance

The current branch adds verified operational behavior for the Azure Blob snapshot provider:

- Startup validates that `PayloadSerializerFormat` resolves to exactly one registered `ISerializationProvider` before the host starts serving requests.
- `ContainerInitializationMode.CreateIfMissing` attempts to create the configured container during startup.
- `ContainerInitializationMode.ValidateExists` fails startup when the configured container does not already exist.
- Duplicate writes do not overwrite an existing snapshot version. The provider surfaces a conflict instead.
- Unreadable stored blobs fail closed instead of returning partially trusted snapshot data.
- `ReadLatestAsync`, `PruneAsync`, and `DeleteAllAsync` remain stream-local prefix scans, so their cost grows with the number of snapshots stored for a single stream.

## Failure Modes And Rollback

Reducer and snapshot diagnostics still follow standard Orleans grain persistence patterns. The verified Blob provider failure cases added on this branch are:

- startup failure because serializer validation does not produce exactly one match
- startup failure because container creation or existence validation fails
- write failure because the requested snapshot version already exists
- read failure because the stored blob frame is corrupt, unsupported, or otherwise unreadable

Refer to [Tributary: Azure Blob Storage Provider](../storage-providers/blob.md) for the exact provider surface.

## Telemetry To Watch

Monitor standard Orleans grain persistence and snapshot metrics for any silo hosting Tributary components.

For the Blob provider specifically, use application logs to confirm startup serializer validation, container initialization behavior, duplicate-write conflicts, unreadable blobs, and maintenance actions such as prune or delete-all.

## Summary

This page establishes the operational boundary for Tributary reducer runtime and adds the verified operational facts introduced by the Azure Blob snapshot provider.

## Next Steps

- Use [Tributary Reference](../reference/reference.md) for the currently verified package surface.
- Use [Tributary: Azure Blob Storage Provider](../storage-providers/blob.md) for exact Blob registration, configuration, and storage-format facts.
- Use [Archived Documentation](../../archived/index.md) for additional preserved material on Tributary operations.
