# Spec: Azure Blob Storage Snapshot Provider (#264)

## Status

**Draft** | Size: **Medium** | Approval Checkpoint: **No** (internal infrastructure, no breaking changes)

## Summary

Implement `ISnapshotStorageProvider` using Azure Blob Storage as an alternative to Cosmos DB for snapshot persistence.

## Links

- **Issue**: [#264](https://github.com/Gibbs-Morris/mississippi/issues/264)
- **Branch**: `feature/264-blob-snapshot-provider`

## Spec Documents

| Document | Purpose |
|----------|---------|
| [learned.md](./learned.md) | Verified repository facts and evidence |
| [rfc.md](./rfc.md) | RFC-style design document |
| [verification.md](./verification.md) | Claims, questions, and verification answers |
| [implementation-plan.md](./implementation-plan.md) | Detailed step-by-step plan |
| [progress.md](./progress.md) | Timestamped progress log |

## Quick Reference

### New Projects

- `src/EventSourcing.Snapshots.Blob/` - Main implementation
- `tests/EventSourcing.Snapshots.Blob.L0Tests/` - Unit tests

### Key Files to Create

- `BlobSnapshotStorageProvider.cs` - Main provider implementing `ISnapshotStorageProvider`
- `BlobSnapshotStorageOptions.cs` - Configuration options
- `BlobSnapshotStorageProviderRegistrations.cs` - DI registration extensions
- `BlobSnapshotStorageLoggerExtensions.cs` - LoggerMessage-based logging
- `Diagnostics/BlobSnapshotStorageMetrics.cs` - OpenTelemetry metrics
- `Compression/` - Compression abstractions and implementations
- `Storage/` - Blob operations and repository

### Keyed Services to Add

- `MississippiDefaults.ServiceKeys.BlobSnapshots` = `"mississippi-blob-snapshots"`
- `MississippiDefaults.ServiceKeys.BlobSnapshotsClient` = `"mississippi-blob-snapshots-client"`
- `MississippiDefaults.ContainerIds.BlobSnapshots` = `"snapshots"` (reuse existing)

## Acceptance Criteria

See [implementation-plan.md](./implementation-plan.md) for full checklist.
