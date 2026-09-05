---
id: tributary-storage-blob
title: "Tributary: Azure Blob Provider"
sidebar_label: Azure Blob
sidebar_position: 2
description: Azure Blob Storage snapshot provider reference for Tributary.
---

# Tributary: Azure Blob Provider

## Overview

The Azure Blob snapshot provider stores each snapshot as one JSON document Blob in a dedicated container. It implements `ISnapshotStorageProvider` and reports `Format` as `azure-blob`.

## Packages

- `Mississippi.Tributary.Runtime.Storage.Blobs`
- `Mississippi.Tributary.Runtime.Storage.Abstractions`

## Registration

Register the provider with `AddBlobSnapshotStorageProvider()` on `IServiceCollection`.

The provider expects a keyed `BlobServiceClient` whose default key is `SnapshotBlobDefaults.BlobServiceClientServiceKey` (`mississippi-blob-snapshots`). Hosts can forward any deployment-specific key to that provider-owned key.

## Configuration Options

Configuration is provided through `SnapshotBlobStorageOptions`:

| Option | Default | Description |
|--------|---------|-------------|
| `BlobServiceClientServiceKey` | `SnapshotBlobDefaults.BlobServiceClientServiceKey` | Keyed service key for resolving `BlobServiceClient` |
| `ContainerName` | `SnapshotBlobDefaults.ContainerName` | Blob container used for snapshots |
| `EnableCompression` | `false` | Enables gzip compression for stored payload bytes |
| `MaximumSnapshotPayloadSizeBytes` | `SnapshotBlobDefaults.DefaultMaximumSnapshotPayloadSizeBytes` | Maximum uncompressed snapshot payload size accepted on read or write |
| `MaximumSnapshotDocumentSizeBytes` | `SnapshotBlobDefaults.DefaultMaximumSnapshotDocumentSizeBytes` | Maximum complete JSON Blob size accepted on read or write, including metadata and Base64 payload |

The default maximum uncompressed payload size is 128 MiB. The default maximum JSON document size is 192 MiB. Both limits must be positive and no greater than `Array.MaxLength`, because this provider materializes snapshots in memory. Increase them together when a domain requires larger snapshots, allowing for Base64 and JSON overhead.

Downloads reject an oversized declared content length and stop reading when the actual JSON body exceeds the document limit. Reads validate declared payload sizes before Base64 decoding and bound gzip expansion. These are per-document limits; concurrent operations and intermediate buffers can consume additional memory.

For production Azure deployments, prefer registering your own keyed `BlobServiceClient` with managed identity or another credential flow and then calling `AddBlobSnapshotStorageProvider()`. The connection-string overload is useful for local development, tests, and simple hosts.

The connection-string overload creates the client under `SnapshotBlobDefaults.BlobServiceClientServiceKey`. Combining that overload with a different `BlobServiceClientServiceKey` fails options validation. To use a custom key, register the client yourself and use an overload without a connection string.

## Container Initialization

The provider registers `SnapshotBlobContainerInitializer` as a hosted service. On host startup it calls `CreateContainerIfNotExistsAsync` for the configured private container. Registration remains synchronous; the container creation happens asynchronously after the host starts.

## Blob Naming

Each snapshot uses this durable Blob path format:

`v1/streams/{SHA256HEX(SnapshotStreamKey.ToString())}/versions/{version:D20}.json`

Notes:

- The provider hashes `SnapshotStreamKey.ToString()` with SHA-256 and uses the uppercase hexadecimal digest.
- Raw `SnapshotKey.ToString()` and raw stream components are not used as Blob names.
- Version names are zero-padded to 20 digits so lexical ordering matches numeric ordering.
- `SnapshotStreamKey.ToString()` includes `ReducersHash`; changing the reducer hash creates a new Blob prefix. Delete or prune old-hash snapshots after a reducer migration if they are no longer needed.

## JSON Document Schema

The Blob body is JSON with these required fields. Missing fields and null values are rejected as invalid snapshot data:

| Field | Meaning |
|-------|---------|
| `schemaVersion` | Document schema version. Current value: `1` |
| `brookName` | `SnapshotStreamKey.BrookName` |
| `snapshotStorageName` | `SnapshotStreamKey.SnapshotStorageName` |
| `entityId` | `SnapshotStreamKey.EntityId` |
| `reducersHash` | `SnapshotStreamKey.ReducersHash` and returned `SnapshotEnvelope.ReducerHash` |
| `version` | Snapshot version |
| `dataContentType` | `SnapshotEnvelope.DataContentType` |
| `dataSizeBytes` | Uncompressed payload size |
| `compression` | `none` or `gzip` |
| `storedSizeBytes` | Stored byte count before Base64 encoding |
| `data` | Base64-encoded stored bytes |

The Blob HTTP content type is `application/json`. The provider does not use Azure HTTP `Content-Encoding`.

## Compression Behavior

Compression is optional.

- When `EnableCompression` is `false`, the provider stores the payload bytes unchanged and writes `compression: none`.
- When `EnableCompression` is `true`, the provider gzip-compresses the payload bytes before Base64 encoding and writes `compression: gzip`.
- `dataSizeBytes` always records the uncompressed payload size.
- `storedSizeBytes` records the stored byte count before Base64 encoding.

## Read Validation

Reads fail closed when stored data does not match the requested snapshot or the stored metadata is invalid. The provider validates:

- `schemaVersion`
- `brookName`
- `snapshotStorageName`
- `entityId`
- `reducersHash`
- `version`
- Base64 payload integrity
- supported `compression` values
- `storedSizeBytes`
- `dataSizeBytes`

When a read succeeds, the returned `SnapshotEnvelope` preserves `ReducerHash`.

Malformed documents raise `InvalidDataException`. A missing Blob returns `null`; other Azure failures and cancellation propagate to the caller. Invalid documents are not treated as cache misses. Gzip size-limit errors preserve the configured-limit diagnostic.

## Persistence and Retention

Writes replace the Blob at the requested snapshot key, matching the existing snapshot provider's upsert behavior. The provider does not add a cross-Blob transaction or a conditional-write concurrency guarantee.

Deletion can target one version or all recognized snapshot Blobs under a stream prefix. Pruning always retains the highest stored version and any versions matching the supplied retention moduli. Stream deletion and pruning list and delete Blobs individually; callers should coordinate maintenance with writers when they require a stable retained set.

## Diagnostics

The provider emits metrics through the `Mississippi.Storage.Blob.Snapshots` meter.

## Spring Sample

`samples/Spring/Spring.Runtime/Program.cs` forwards the Aspire `blobs` resource to `SnapshotBlobDefaults.BlobServiceClientServiceKey` and registers `AddBlobSnapshotStorageProvider(options => options.EnableCompression = true);`.

Brooks event storage remains on Cosmos DB.

Switching provider registration does not migrate existing Cosmos snapshots or enable fallback reads from Cosmos. When no Blob snapshot exists, the existing Tributary runtime reconstructs state from retained base snapshots and events, then requests background persistence. Existing Cosmos snapshot documents remain in their original store.

## Next Steps

- [Storage Providers Overview](index.md)
- [Tributary Operations](../operations/operations.md)
- [Tributary Troubleshooting](../troubleshooting/troubleshooting.md)
