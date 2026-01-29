# Learned: Repository Facts for Blob Snapshot Provider

## Verified Facts

### Snapshot Abstractions

| File | Evidence |
|------|----------|
| `src/EventSourcing.Snapshots.Abstractions/ISnapshotStorageProvider.cs` | Combines `ISnapshotStorageReader` + `ISnapshotStorageWriter`, exposes `string Format` property |
| `src/EventSourcing.Snapshots.Abstractions/ISnapshotStorageReader.cs` | `ReadAsync(SnapshotKey, CancellationToken)` returns `Task<SnapshotEnvelope?>` |
| `src/EventSourcing.Snapshots.Abstractions/ISnapshotStorageWriter.cs` | `WriteAsync`, `DeleteAsync`, `DeleteAllAsync`, `PruneAsync` methods |
| `src/EventSourcing.Snapshots.Abstractions/SnapshotKey.cs` | `SnapshotStreamKey Stream` + `long Version` |
| `src/EventSourcing.Snapshots.Abstractions/SnapshotStreamKey.cs` | `BrookName`, `SnapshotStorageName`, `EntityId`, `ReducersHash` |
| `src/EventSourcing.Snapshots.Abstractions/SnapshotEnvelope.cs` | `ImmutableArray<byte> Data`, `string DataContentType`, `long DataSizeBytes`, `string ReducerHash` |

### Cosmos Snapshot Provider (Reference Implementation)

| File | Purpose |
|------|---------|
| `src/EventSourcing.Snapshots.Cosmos/SnapshotStorageProvider.cs` | Main provider, `Format = "cosmos-db"`, delegates to `ISnapshotCosmosRepository` |
| `src/EventSourcing.Snapshots.Cosmos/SnapshotStorageOptions.cs` | Options: `ContainerId`, `DatabaseId`, `CosmosClientServiceKey`, `QueryBatchSize` |
| `src/EventSourcing.Snapshots.Cosmos/SnapshotStorageProviderRegistrations.cs` | DI extensions, keyed container registration, hosted service for initialization |
| `src/EventSourcing.Snapshots.Cosmos/SnapshotStorageProviderLoggerExtensions.cs` | `[LoggerMessage]` partial methods for all operations |
| `src/EventSourcing.Snapshots.Cosmos/Diagnostics/SnapshotStorageMetrics.cs` | OpenTelemetry metrics: counters, histograms with tags |
| `src/EventSourcing.Snapshots.Cosmos/Storage/SnapshotCosmosRepository.cs` | Domain-level operations, delegates to `ISnapshotContainerOperations` |
| `src/EventSourcing.Snapshots.Cosmos/ISnapshotContainerOperations.cs` | Low-level SDK abstraction |

### MississippiDefaults (Common.Abstractions)

| Constant | Value | Purpose |
|----------|-------|---------|
| `ServiceKeys.CosmosSnapshots` | `"mississippi-cosmos-snapshots"` | Keyed container for Cosmos snapshots |
| `ServiceKeys.CosmosSnapshotsClient` | `"mississippi-cosmos-snapshots-client"` | Keyed CosmosClient |
| `ServiceKeys.BlobLocking` | `"mississippi-blob-locking"` | Already exists for blob locking |
| `ContainerIds.Snapshots` | `"snapshots"` | Default container name |

### Package Dependencies

| Package | Version | Already in CPM |
|---------|---------|----------------|
| `Azure.Storage.Blobs` | `12.27.0` | ✅ Yes |
| `Aspire.Azure.Storage.Blobs` | `13.1.0` | ✅ Yes |

### Project Structure Patterns

- Projects under `src/` follow `{Product}.{Feature}` naming
- Tests under `tests/` follow `{Project}.L0Tests` naming
- Project files use `<PackageReference Include="X"/>` (no Version - CPM)
- DI registrations in `*Registrations.cs` static classes
- LoggerExtensions use `[LoggerMessage]` partial methods

### Test Patterns

- L0 tests use NSubstitute for mocking
- FluentAssertions for assertions
- Tests organized per-class with `{ClassName}Tests.cs`

## UNVERIFIED

- Blob batch delete API usage patterns (need to verify `BlobBatchClient` behavior)
- Compression library integration (standard `System.IO.Compression` vs external)
