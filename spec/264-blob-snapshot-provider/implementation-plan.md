# Implementation Plan: Azure Blob Snapshot Provider

## Overview

Implement `EventSourcing.Snapshots.Blob` following patterns from `EventSourcing.Snapshots.Cosmos`.

## Phase 1: Foundation (Project Setup & Core Types)

### 1.1 Add Keyed Service Constants to MississippiDefaults

**File:** `src/Common.Abstractions/MississippiDefaults.cs`

Add to `ServiceKeys`:
```csharp
public const string BlobSnapshots = "mississippi-blob-snapshots";
public const string BlobSnapshotsClient = "mississippi-blob-snapshots-client";
```

### 1.2 Create Project Structure

**New Project:** `src/EventSourcing.Snapshots.Blob/`

```
EventSourcing.Snapshots.Blob/
├── EventSourcing.Snapshots.Blob.csproj
├── BlobSnapshotStorageProvider.cs
├── BlobSnapshotStorageOptions.cs
├── BlobSnapshotStorageProviderRegistrations.cs
├── BlobSnapshotStorageProviderLoggerExtensions.cs
├── BlobContainerInitializer.cs
├── SnapshotCompression.cs
├── IBlobSnapshotRepository.cs
├── Compression/
│   ├── ISnapshotCompressor.cs
│   ├── NoCompressionCompressor.cs
│   ├── GZipSnapshotCompressor.cs
│   └── BrotliSnapshotCompressor.cs
├── Diagnostics/
│   └── BlobSnapshotStorageMetrics.cs
└── Storage/
    ├── IBlobSnapshotOperations.cs
    ├── BlobSnapshotOperations.cs
    ├── BlobSnapshotRepository.cs
    ├── BlobSnapshotStorageLoggerExtensions.cs
    └── BlobPathBuilder.cs
```

### 1.3 Create Project File

**File:** `src/EventSourcing.Snapshots.Blob/EventSourcing.Snapshots.Blob.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <ItemGroup>
        <InternalsVisibleTo Include="$(AssemblyName).L0Tests"/>
    </ItemGroup>
    <ItemGroup>
        <PackageReference Include="Azure.Storage.Blobs"/>
        <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions"/>
        <PackageReference Include="Microsoft.Extensions.Options"/>
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include="..\Common.Abstractions\Common.Abstractions.csproj" />
        <ProjectReference Include="..\EventSourcing.Snapshots.Abstractions\EventSourcing.Snapshots.Abstractions.csproj" />
    </ItemGroup>
</Project>
```

## Phase 2: Compression Layer

### 2.1 Compression Enum

**File:** `SnapshotCompression.cs`

```csharp
public enum SnapshotCompression
{
    None,
    GZip,
    Brotli
}
```

### 2.2 Compressor Interface & Implementations

**File:** `Compression/ISnapshotCompressor.cs`
- `Task<byte[]> CompressAsync(ReadOnlyMemory<byte>, CancellationToken)`
- `Task<byte[]> DecompressAsync(ReadOnlyMemory<byte>, CancellationToken)`
- `string ContentEncoding { get; }`

**Files:** `NoCompressionCompressor.cs`, `GZipSnapshotCompressor.cs`, `BrotliSnapshotCompressor.cs`

## Phase 3: Storage Layer

### 3.1 Blob Path Builder

**File:** `Storage/BlobPathBuilder.cs`

Static class to build blob paths:
```
{brookName}/{snapshotStorageName}/{entityId}/{reducersHash}/{version}.snapshot
```

### 3.2 Blob Operations Interface

**File:** `Storage/IBlobSnapshotOperations.cs`

Low-level SDK wrapper:
- `Task<BlobDownloadResult?> DownloadAsync(string path, CancellationToken)`
- `Task UploadAsync(string path, byte[] data, IDictionary<string,string> metadata, AccessTier tier, CancellationToken)`
- `Task DeleteAsync(string path, CancellationToken)`
- `IAsyncEnumerable<string> ListBlobsAsync(string prefix, CancellationToken)`
- `Task DeleteBatchAsync(IEnumerable<string> paths, CancellationToken)`

### 3.3 Blob Operations Implementation

**File:** `Storage/BlobSnapshotOperations.cs`

Wraps `BlobContainerClient`, handles 404 as null.

### 3.4 Blob Repository Interface

**File:** `IBlobSnapshotRepository.cs`

Domain-level interface:
- `Task<SnapshotEnvelope?> ReadAsync(SnapshotKey, CancellationToken)`
- `Task WriteAsync(SnapshotKey, SnapshotEnvelope, CancellationToken)`
- `Task DeleteAsync(SnapshotKey, CancellationToken)`
- `Task DeleteAllAsync(SnapshotStreamKey, CancellationToken)`
- `Task PruneAsync(SnapshotStreamKey, IReadOnlyCollection<int>, CancellationToken)`

### 3.5 Blob Repository Implementation

**File:** `Storage/BlobSnapshotRepository.cs`

Implements domain logic:
- Builds paths using `BlobPathBuilder`
- Compresses/decompresses using `ISnapshotCompressor`
- Delegates to `IBlobSnapshotOperations`
- Detects compression from metadata on read

## Phase 4: Provider & Observability

### 4.1 Options Class

**File:** `BlobSnapshotStorageOptions.cs`

```csharp
public sealed class BlobSnapshotStorageOptions
{
    public string ContainerName { get; set; } = MississippiDefaults.ContainerIds.Snapshots;
    public string BlobServiceClientKey { get; set; } = MississippiDefaults.ServiceKeys.BlobSnapshotsClient;
    public SnapshotCompression WriteCompression { get; set; } = SnapshotCompression.Brotli;
    public AccessTier DefaultAccessTier { get; set; } = AccessTier.Hot;
    public int MaxConcurrency { get; set; } = 10;
}
```

### 4.2 Metrics

**File:** `Diagnostics/BlobSnapshotStorageMetrics.cs`

Following Cosmos pattern:
- `blob.snapshot.read.count` / `read.duration`
- `blob.snapshot.write.count` / `write.duration`
- `blob.snapshot.delete.count`
- `blob.snapshot.prune.count`
- `blob.snapshot.size`
- `blob.snapshot.compression.ratio` (new)

Tags: `snapshot.type`, `result`, `compression`, `tier`

### 4.3 Logger Extensions

**File:** `BlobSnapshotStorageProviderLoggerExtensions.cs`

`[LoggerMessage]` methods:
- `ReadingSnapshot`, `SnapshotFound`, `SnapshotNotFound`
- `WritingSnapshot`, `SnapshotWritten`
- `DeletingSnapshot`, `DeletingAllSnapshots`
- `PruningSnapshots`
- `CompressingSnapshot`, `DecompressingSnapshot`

### 4.4 Main Provider

**File:** `BlobSnapshotStorageProvider.cs`

```csharp
internal sealed class BlobSnapshotStorageProvider : ISnapshotStorageProvider
{
    public string Format => "azure-blob";
    
    // Delegates to IBlobSnapshotRepository
    // Logs entry/exit
    // Records metrics
}
```

## Phase 5: DI Registration & Container Initialization

### 5.1 Container Initializer

**File:** `BlobContainerInitializer.cs`

```csharp
internal sealed class BlobContainerInitializer : IHostedService
{
    // Creates container if not exists on startup
    // Idempotent - safe for multi-node deployments
    // Uses BlobContainerClient.CreateIfNotExistsAsync()
}
```

### 5.2 Registration Extensions

**File:** `BlobSnapshotStorageProviderRegistrations.cs`

Multiple overloads following Cosmos pattern:
- `AddBlobSnapshotStorageProvider()` - uses externally registered client
- `AddBlobSnapshotStorageProvider(string connectionString, Action<BlobSnapshotStorageOptions>?)` - creates keyed client
- `AddBlobSnapshotStorageProvider(IConfiguration, Action<BlobSnapshotStorageOptions>?)` - from config

Registers:
- `BlobContainerInitializer` as `IHostedService`
- Keyed `BlobContainerClient` with `MississippiDefaults.ServiceKeys.BlobSnapshots`
- All internal services (operations, repository, compressors)

## Phase 6: Solution Integration

### 6.1 Add to Solution

Update `mississippi.slnx` to include new project.

### 6.2 Verify Build

```powershell
pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1
pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1
```

## Phase 7: Test Project

### 7.1 Create Test Project

**New Project:** `tests/EventSourcing.Snapshots.Blob.L0Tests/`

```
EventSourcing.Snapshots.Blob.L0Tests/
├── EventSourcing.Snapshots.Blob.L0Tests.csproj
├── BlobSnapshotStorageProviderTests.cs
├── BlobSnapshotStorageOptionsTests.cs
├── BlobSnapshotStorageProviderRegistrationsTests.cs
├── BlobSnapshotRepositoryTests.cs
├── BlobSnapshotOperationsTests.cs
├── BlobPathBuilderTests.cs
├── Compression/
│   ├── GZipSnapshotCompressorTests.cs
│   ├── BrotliSnapshotCompressorTests.cs
│   └── CompressionDetectionTests.cs
└── Diagnostics/
    └── BlobSnapshotStorageMetricsTests.cs
```

### 7.2 Test Coverage Goals

- ≥95% line coverage
- All public methods tested
- Edge cases: null returns, 404 handling, compression detection
- Pruning logic with moduli

## Phase 8: Quality Gates

### 8.1 Run Full Test Suite

```powershell
pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1
```

### 8.2 Run Mutation Testing

```powershell
pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1
```

### 8.3 Final Verification

```powershell
pwsh ./go.ps1
```

## Detailed File Checklist

| # | File | Status |
|---|------|--------|
| 1 | `src/Common.Abstractions/MississippiDefaults.cs` (update) | ⬜ |
| 2 | `src/EventSourcing.Snapshots.Blob/EventSourcing.Snapshots.Blob.csproj` | ⬜ |
| 3 | `src/EventSourcing.Snapshots.Blob/SnapshotCompression.cs` | ⬜ |
| 4 | `src/EventSourcing.Snapshots.Blob/BlobSnapshotStorageOptions.cs` | ⬜ |
| 5 | `src/EventSourcing.Snapshots.Blob/Compression/ISnapshotCompressor.cs` | ⬜ |
| 6 | `src/EventSourcing.Snapshots.Blob/Compression/NoCompressionCompressor.cs` | ⬜ |
| 7 | `src/EventSourcing.Snapshots.Blob/Compression/GZipSnapshotCompressor.cs` | ⬜ |
| 8 | `src/EventSourcing.Snapshots.Blob/Compression/BrotliSnapshotCompressor.cs` | ⬜ |
| 9 | `src/EventSourcing.Snapshots.Blob/Storage/BlobPathBuilder.cs` | ⬜ |
| 10 | `src/EventSourcing.Snapshots.Blob/Storage/IBlobSnapshotOperations.cs` | ⬜ |
| 11 | `src/EventSourcing.Snapshots.Blob/Storage/BlobSnapshotOperations.cs` | ⬜ |
| 12 | `src/EventSourcing.Snapshots.Blob/Storage/BlobSnapshotStorageLoggerExtensions.cs` | ⬜ |
| 13 | `src/EventSourcing.Snapshots.Blob/Storage/BlobSnapshotRepository.cs` | ⬜ |
| 14 | `src/EventSourcing.Snapshots.Blob/IBlobSnapshotRepository.cs` | ⬜ |
| 15 | `src/EventSourcing.Snapshots.Blob/BlobContainerInitializer.cs` | ⬜ |
| 16 | `src/EventSourcing.Snapshots.Blob/Diagnostics/BlobSnapshotStorageMetrics.cs` | ⬜ |
| 17 | `src/EventSourcing.Snapshots.Blob/BlobSnapshotStorageProviderLoggerExtensions.cs` | ⬜ |
| 18 | `src/EventSourcing.Snapshots.Blob/BlobSnapshotStorageProvider.cs` | ⬜ |
| 19 | `src/EventSourcing.Snapshots.Blob/BlobSnapshotStorageProviderRegistrations.cs` | ⬜ |
| 20 | `mississippi.slnx` (update) | ⬜ |
| 21 | `tests/EventSourcing.Snapshots.Blob.L0Tests/EventSourcing.Snapshots.Blob.L0Tests.csproj` | ⬜ |
| 22 | Test files (multiple) | ⬜ |

## Acceptance Criteria

- [ ] Implements `ISnapshotStorageProvider` interface completely
- [ ] `Format` property returns `"azure-blob"`
- [ ] Supports GZip, Brotli, and no compression for writes (configurable)
- [ ] Auto-detects compression on reads via blob metadata
- [ ] Supports configurable access tier (Hot, Cool, Cold)
- [ ] Follows keyed services pattern with `MississippiDefaults`
- [ ] Full OpenTelemetry metrics with counters, histograms, and tags
- [ ] LoggerExtensions with `[LoggerMessage]` for all operations
- [ ] Efficient pruning using blob listing and batch delete
- [ ] Container initialization on startup
- [ ] L0 tests with ≥95% coverage
- [ ] Mutation testing score maintained/improved
- [ ] Zero compiler/analyzer warnings
- [ ] XML documentation on all public types/members
