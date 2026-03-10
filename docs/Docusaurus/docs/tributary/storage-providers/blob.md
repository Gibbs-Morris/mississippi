---
id: tributary-storage-blob
title: "Tributary: Blob Storage Provider"
sidebar_label: Blob Storage
sidebar_position: 3
description: Azure Blob Storage provider for Tributary snapshot persistence.
---

# Tributary: Blob Storage Provider

## Overview

The Blob storage snapshot provider persists Tributary snapshot envelopes to Azure Blob Storage containers using a deterministic path built from the snapshot stream key and version.

## Packages

- `Mississippi.Tributary.Runtime.Storage.Blob`
- `Mississippi.Tributary.Runtime.Storage.Abstractions`

## Registration

Use one of the `AddBlobSnapshotStorageProvider(...)` overloads on `IServiceCollection`.

### Register with a connection string

```csharp
builder.Services.AddBlobSnapshotStorageProvider(
    blobStorageConnectionString,
    options =>
    {
        options.ContainerName = "snapshots";
        options.CompressionEnabled = true;
    });
```

### Register with a keyed `BlobServiceClient`

```csharp
builder.Services.AddKeyedSingleton(
    SnapshotBlobDefaults.BlobServiceClientServiceKey,
    (
        _,
        _
    ) => new BlobServiceClient(blobStorageConnectionString));

builder.Services.AddBlobSnapshotStorageProvider(options =>
{
    options.BlobServiceClientServiceKey = SnapshotBlobDefaults.BlobServiceClientServiceKey;
    options.ContainerName = "snapshots";
    options.CompressionEnabled = false;
});
```

The keyed-client path matches the framework-host wiring used in `tests/Tributary.Runtime.Storage.Blob.L2Tests/BlobSnapshotStorageFixture.cs`.

## Configuration Options

Configuration is provided through `SnapshotBlobStorageOptions`:

| Option | Default | Description |
|--------|---------|-------------|
| `BlobServiceClientServiceKey` | `SnapshotBlobDefaults.BlobServiceClientServiceKey` | Keyed service key used to resolve `BlobServiceClient` |
| `CompressionEnabled` | `false` | Enables gzip compression before upload |
| `ContainerName` | `SnapshotBlobDefaults.ContainerName` | Blob container used for snapshots |

## Blob Layout

Each snapshot is stored beneath a deterministic Blob path:

```text
<brookName>/<snapshotStorageName>/<entityId>/<reducersHash>/<version>.snapshot
```

Each path segment is URI-escaped before upload, so entity identifiers that contain characters such as `/` remain safe to store and list.

## Container Initialization

The provider registers an internal hosted service that calls `CreateIfNotExistsAsync` for the configured container during host startup. That behavior is exercised end-to-end in `tests/Tributary.Runtime.Storage.Blob.L2Tests/BlobSnapshotStorageTests.cs`.

## Aspire and Azurite Verification

The repository includes an Azurite-backed Aspire AppHost dedicated to Blob snapshot provider verification:

- AppHost: `tests/Tributary.Runtime.Storage.Blob.L2Tests.AppHost/Program.cs`
- L2 fixture and tests: `tests/Tributary.Runtime.Storage.Blob.L2Tests/`

The AppHost wiring pattern is:

```csharp
IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
IResourceBuilder<AzureStorageResource> storage = builder.AddAzureStorage("storage").RunAsEmulator();
_ = storage.AddBlobs("blobs");
await builder.Build().RunAsync();
```

That path is opt-in and activates when `MISSISSIPPI_TRIBUTARY_BLOB_AZURITE_L2=true` is set in an Aspire-capable environment. It requires no live Azure credentials, but it does require the local environment to support Aspire DCP orchestration.

## Opt-In Live Azure Smoke Tests

The L2 project also includes a live Azure Blob smoke path that activates only when the following environment variables are set:

| Variable | Purpose |
|----------|---------|
| `MISSISSIPPI_TRIBUTARY_BLOB_AZURITE_L2` | Enables the Azurite/AppHost-backed Blob L2 tests |
| `MISSISSIPPI_TRIBUTARY_BLOB_LIVE_CONNECTION_STRING` | Connection string for the target Blob account |
| `MISSISSIPPI_TRIBUTARY_BLOB_LIVE_CONTAINER` | Optional container name override for the smoke test |

When these variables are absent, the corresponding test paths skip cleanly.

## Diagnostics

The provider emits snapshot metrics through the shared `Mississippi.Storage.Snapshots` meter with Blob-prefixed instrument names such as:

- `blob.snapshot.read.count`
- `blob.snapshot.read.duration`
- `blob.snapshot.write.count`
- `blob.snapshot.write.duration`
- `blob.snapshot.delete.count`
- `blob.snapshot.prune.count`
- `blob.snapshot.size`

The provider also uses `LoggerMessage`-based logging for repository, container, and provider operations.

## Summary

The Blob storage provider gives Tributary snapshot persistence a Blob-native option with deterministic paths, optional gzip compression, startup container initialization, Azurite-backed integration coverage, and an opt-in live Azure smoke path.

## Next Steps

- [Storage Providers Overview](index.md)
- [Tributary Operations](../operations/operations.md)
- [Tributary Troubleshooting](../troubleshooting/troubleshooting.md)
