---
id: tributary-storage-blob
title: "Tributary: Azure Blob Storage Provider"
sidebar_label: Azure Blob
sidebar_position: 3
description: Exact registration, configuration, and runtime facts for the Tributary Azure Blob snapshot storage provider.
---

# Tributary: Azure Blob Storage Provider

## Overview

Use this page when you need the exact registration surface, configuration options, and verified runtime behavior for the Azure Blob-backed Tributary snapshot storage provider.

The provider lives in `Mississippi.Tributary.Runtime.Storage.Blob`, registers as the `azure-blob` snapshot storage format, and persists Tributary snapshot envelopes into an Azure Blob container.

## Packages

- `Mississippi.Tributary.Runtime.Storage.Blob`
- `Mississippi.Tributary.Runtime.Storage.Abstractions`
- `Mississippi.Brooks.Serialization.Abstractions`

## Registration Surface

The provider exposes these `IServiceCollection` extension methods:

| Method | Use when |
| --- | --- |
| `AddBlobSnapshotStorageProvider()` | You already registered a keyed `BlobServiceClient` and configured `SnapshotBlobStorageOptions` separately. |
| `AddBlobSnapshotStorageProvider(string blobConnectionString, Action<SnapshotBlobStorageOptions>? configureOptions = null)` | You want the provider to create the keyed `BlobServiceClient` from a connection string. |
| `AddBlobSnapshotStorageProvider(Action<SnapshotBlobStorageOptions> configureOptions)` | You already registered the keyed `BlobServiceClient` and want to configure options in code. |
| `AddBlobSnapshotStorageProvider(IConfiguration configuration)` | You already registered the keyed `BlobServiceClient` and want to bind options from configuration. |

This verified sample from the Crescent L2 trust-slice tests shows the connection-string overload with explicit provider options:

```csharp
builder.Services.AddBlobSnapshotStorageProvider(
    blobConnectionString,
    options =>
    {
        options.ContainerName = snapshotContainerName;
        options.BlobPrefix = snapshotBlobPrefix;
        options.Compression = SnapshotBlobCompression.Gzip;
        options.PayloadSerializerFormat = CrescentBlobCustomJsonSerializationProvider.SerializerFormat;
        options.ContainerInitializationMode = SnapshotBlobContainerInitializationMode.CreateIfMissing;
    });
```

## Configuration Options

`SnapshotBlobStorageOptions` controls the provider.

| Option | Default | Verified constraint or behavior |
| --- | --- | --- |
| `ContainerInitializationMode` | `CreateIfMissing` | Must be a supported enum value. Startup either creates the container or validates that it already exists. |
| `ContainerName` | `snapshots` | Must be non-empty. Determines the target Blob container. |
| `BlobPrefix` | `snapshots/` | Used as the logical root prefix before the hashed stream segment. |
| `Compression` | `Off` | Supported values are `Off` and `Gzip`. Compression applies only to the payload segment. |
| `BlobServiceClientServiceKey` | `mississippi-blob-snapshots-client` | Must be non-empty. Used to resolve the keyed `BlobServiceClient`. |
| `ListPageSizeHint` | `500` | Must be greater than zero. Forwarded to stream-local Blob listing operations. |
| `MaxHeaderBytes` | `65536` | Must be greater than zero. Decode fails closed when a stored header exceeds this limit. |
| `PayloadSerializerFormat` | `System.Text.Json` | Must be non-empty and resolve to exactly one registered `ISerializationProvider` by `provider.Format`. |

## Startup Validation

The provider adds a hosted startup initializer. Before any snapshot operation runs, startup performs these checks:

1. Resolve exactly one `ISerializationProvider` whose `Format` matches `PayloadSerializerFormat`.
2. If `ContainerInitializationMode` is `CreateIfMissing`, call `CreateIfNotExistsAsync` for the configured container.
3. If `ContainerInitializationMode` is `ValidateExists`, call `ExistsAsync` and fail startup if the container is missing.

Startup fails when:

- the keyed `BlobServiceClient` registration is missing
- no serializer matches `PayloadSerializerFormat`
- more than one serializer matches `PayloadSerializerFormat`
- container creation or existence validation fails

## Naming And Stored Format

The provider uses a deterministic Blob naming contract for each logical snapshot stream:

- canonical stream identity is JSON with `brookName`, `snapshotStorageName`, `entityId`, and `reducersHash`
- stream prefix is `{BlobPrefix}{SHA256(canonicalStreamIdentity)}/`
- blob name is `{streamPrefix}v{version:D20}.snapshot`

Each stored blob contains a provider-owned frame with these verified characteristics:

- fixed `TRIBSNAP` magic bytes and an explicit frame version
- an uncompressed JSON header
- a payload segment containing the snapshot bytes
- optional `gzip` compression for the payload segment only
- a SHA-256 checksum over the uncompressed payload bytes
- the concrete persisted serializer identity used to create the payload bytes

## Runtime Behavior

The current branch verifies these user-visible behaviors:

- `ISnapshotStorageProvider.Format` is `azure-blob`
- duplicate version writes use conditional create semantics and throw `SnapshotBlobDuplicateVersionException` instead of overwriting an existing blob
- unreadable stored blobs fail closed with `SnapshotBlobUnreadableFrameException`
- `ReadLatestAsync`, `PruneAsync`, and `DeleteAllAsync` enumerate only the hashed prefix for the target stream
- `ReadLatestAsync`, `PruneAsync`, and `DeleteAllAsync` remain linear in the number of snapshots stored for that stream
- `DeleteAsync` is idempotent when the target snapshot blob does not exist

## Operational Notes

The provider currently emits logs for:

- startup serializer validation
- container creation or validation
- duplicate-write conflicts
- unreadable stored blobs
- prune and delete-all maintenance actions

The current branch does not introduce manifest-style indexing or pointer blobs. Latest-version discovery and maintenance continue to rely on stream-local prefix scans.

## Summary

The Tributary Azure Blob provider gives Tributary snapshot storage a keyed `BlobServiceClient` registration path, explicit startup validation, deterministic Blob naming, an inspectable provider-owned frame, and fail-closed behavior for duplicate writes and unreadable stored blobs.

## Next Steps

- [Storage Providers Overview](./index.md)
- [Tributary Operations](../operations/operations.md)
- [Tributary Reference](../reference/reference.md)
- [ADR-0001: Use Canonical Stream Identity and Hashed Blob Naming for Snapshot Blobs](../../adr/0001-use-canonical-stream-identity-and-hashed-blob-naming-for-snapshot-blobs.md)
- [ADR-0005: Use Conditional Blob Creation and Stream-Local Maintenance Scans](../../adr/0005-use-conditional-blob-creation-and-stream-local-maintenance-scans.md)
- [ADR-0006: Use Configurable Container Initialization for Blob Snapshot Storage](../../adr/0006-use-configurable-container-initialization-for-blob-snapshot-storage.md)

## Source Code

- [Blob provider package](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime.Storage.Blob/Tributary.Runtime.Storage.Blob.csproj)
- [Registration methods](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProviderRegistrations.cs)
- [Options](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageOptions.cs)
- [Startup validation](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime.Storage.Blob/Startup/BlobContainerInitializer.cs)
- [Serializer resolution](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime.Storage.Blob/Startup/SnapshotPayloadSerializerResolver.cs)
- [Blob naming](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime.Storage.Blob/Naming/BlobNameStrategy.cs)
- [Stored frame codec](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime.Storage.Blob/Storage/BlobEnvelopeCodec.cs)
- [Repository behavior](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Tributary.Runtime.Storage.Blob/Storage/SnapshotBlobRepository.cs)
- [Verified sample registration](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs)
- [Blob provider L0 tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Tributary.Runtime.Storage.Blob.L0Tests)
