---
id: tributary-storage-blob
title: Tributary Azure Blob Provider
description: Reference the Blob snapshot provider's runtime composition, options, durable format, limits, and failure behavior.
sidebar_label: Azure Blob
sidebar_position: 3
---

# Tributary Azure Blob Provider

The Blob provider persists Tributary snapshot envelopes as one Azure Blob per snapshot version. This page records its
configuration and storage contract.

## Applies to

- Package: `Mississippi.Tributary.Runtime.Storage.Blob`
- Runtime entry point: `IRuntimeBuilder.AddBlobSnapshotStorageProvider(...)`
- Storage interface: `ISnapshotStorageProvider` and its reader and writer roles

Brooks event history is configured separately. The [Cosmos snapshot provider](cosmos.md) remains available.

## Runtime composition

Call `AddBlobSnapshotStorageProvider` inside `UseMississippi(...)`. The preferred mode uses a host-owned keyed
`BlobServiceClient`. This excerpt follows the keyed-client forwarding and staged runtime pattern used by Spring:

```csharp
builder.Services.AddKeyedSingleton<BlobServiceClient>(
    SnapshotBlobDefaults.BlobServiceClientServiceKey,
    (sp, _) => sp.GetRequiredKeyedService<BlobServiceClient>("blobs"));

builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseMississippi(runtime =>
    {
        runtime.AddBlobSnapshotStorageProvider();
        runtime.ApplyToSilo(siloBuilder);
    });
});
```

The host must register `"blobs"` before this excerpt. Provide its Azure Blob endpoint and credentials through the host's
normal configuration. In an application already using `UseMississippi`, add the Blob call to its existing callback rather
than adding another runtime root. The connection-string overload creates a lazy keyed client under the default key.

| Overload | Client source | Options source |
| --- | --- | --- |
| `AddBlobSnapshotStorageProvider(Action<SnapshotBlobStorageOptions>?)` | Host-owned keyed client | Optional callback |
| `AddBlobSnapshotStorageProvider(string, Action<SnapshotBlobStorageOptions>?)` | Connection string | Optional callback |
| `AddBlobSnapshotStorageProvider(IConfiguration)` | Host-owned keyed client | Bound section |
| `AddBlobSnapshotStorageProvider(string, IConfiguration)` | Connection string | Bound section |

The provider registers a hosted initializer that creates the configured container if absent when the host starts.
Composition itself does not contact Blob Storage. A connection string cannot be combined with a custom client key.

## Options and limits

| Option | Default | Constraint or effect |
| --- | --- | --- |
| `BlobServiceClientServiceKey` | `mississippi-blob-snapshots` | Nonempty key used to resolve a host-owned client. |
| `ContainerName` | `snapshots` | Valid lowercase Azure container name, 3–63 characters. |
| `EnableCompression` | `false` | Gzip-compresses payload bytes when enabled; reads use stored metadata. |
| `MaximumSnapshotPayloadSizeBytes` | 128 MiB | Positive, at most `Array.MaxLength`; bounds uncompressed payloads. |
| `MaximumSnapshotDocumentSizeBytes` | 192 MiB | Positive, at most `Array.MaxLength`; bounds the complete JSON Blob. |

Changing the client or container can point the host at different stored data. No migration or cross-provider fallback is
performed.

## Durable format and behavior

A stream prefix is `v1/streams/{SHA256-uppercase-hex}/versions/`, where SHA-256 hashes the UTF-8 text form of the
`SnapshotStreamKey` in its current component order: brook name, snapshot storage name, entity ID, reducers hash. A Blob
name appends a zero-padded 20-digit version and `.json`. Keep these logical identity components stable for persisted
snapshots. The JSON document stores identity, version, MIME type, uncompressed length, compression name, stored length,
and Base64 payload.

- `ReadAsync` returns `null` only for a missing Blob. Malformed JSON, identity mismatch, unsupported compression, or
  exceeded limits fail with `InvalidDataException`; other storage errors propagate.
- `WriteAsync` creates a new version conditionally. A definite existing-version response raises
  `SnapshotBlobDuplicateVersionException`; the provider does not overwrite it.
- If a write times out or is cancelled after reaching Azure, its commit outcome may be unknown. Check the named version
  before retrying. A retry that encounters an existing version is a conflict, not proof that the previous caller received
  success.
- `DeleteAsync` and `DeleteAllAsync` are idempotent for missing Blobs. `PruneAsync` retains the greatest listed version
  and versions divisible by any configured nonzero modulus. Listing and deletion are stream-scoped.
- Blob operations use the caller's cancellation token. The provider does not copy Cosmos snapshots into Blob Storage.

## Next steps

- [Cosmos snapshot provider](cosmos.md) for the alternative backend.
- [Snapshot retention](../reference/snapshot-retention.md) for runtime policy selection.
- [Storage provider overview](index.md) for the shared reader and writer contracts.
