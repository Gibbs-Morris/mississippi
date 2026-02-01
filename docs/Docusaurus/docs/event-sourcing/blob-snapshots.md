---
id: blob-snapshots
title: Blob Snapshot Storage
sidebar_label: Blob Storage
---

# Blob Snapshot Storage

The `Mississippi.EventSourcing.Snapshots.Blob` library provides an implementation of `ISnapshotStorageProvider` that persists aggregate snapshots to Azure Blob Storage.

## Installation

```bash
dotnet add package Mississippi.EventSourcing.Snapshots.Blob
```

## Registration

Register the provider using the `AddBlobSnapshotStorageProvider` extension method on `IServiceCollection`.

```csharp
using Mississippi.EventSourcing.Snapshots.Blob;

// Registration with default options
builder.Services.AddBlobSnapshotStorageProvider(
    "UseDevelopmentStorage=true"
);

// Registration with custom options
builder.Services.AddBlobSnapshotStorageProvider(
    "UseDevelopmentStorage=true",
    options =>
    {
        options.ContainerName = "my-snapshots";
        options.DefaultAccessTier = Azure.Storage.Blobs.Models.AccessTier.Cool;
    }
);
```

Or, if you already have a `BlobServiceClient` configured under a keyed service:

```csharp
builder.Services.AddKeyedSingleton<BlobServiceClient>(
    MississippiDefaults.ServiceKeys.BlobSnapshotsClient,
    new BlobServiceClient("...")
);

builder.Services.AddBlobSnapshotStorageProvider();
```

## Configuration

The `BlobSnapshotStorageOptions` class governs the behavior of the provider.

| Property | Default | Description |
|:---|:---|:---|
| `ContainerName` | `snapshots` | The name of the blob container. |
| `DefaultAccessTier` | `Hot` | The access tier for new snapshots (Hot, Cool, Cold). Archive is not supported. |
| `WriteCompression` | `Brotli` | Compression algorithm to use (None, GZip, Brotli). |

### Example appsettings.json

```json
{
  "EventSourcing": {
    "Snapshots": {
      "Blob": {
        "ContainerName": "production-snapshots",
        "DefaultAccessTier": "Cool",
        "WriteCompression": "Brotli"
      }
    }
  }
}
```

## Features

- **Compression**: Supports GZip and Brotli transparent compression.
- **Metadata**: Stores snapshot metadata (sequence number, content type, compression algorithm) as blob metadata.
- **Pruning**: Efficiently prunes old snapshots based on retention policies.
- **Metrics**: Emits standard diagnostics for storage operations.

## Dependency Injection

The provider relies on the following services:

- `IBlobSnapshotRepository`: Handles low-level blob operations.
- `ISnapshotCompressor`: Handles compression/decompression.
- `BlobContainerInitializer`: Ensures the container exists at startup (Hosted Service).
