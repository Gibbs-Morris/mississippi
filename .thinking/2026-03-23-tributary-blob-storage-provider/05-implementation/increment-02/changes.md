# Increment 2 Changes

## Implementation Summary

- Restored the increment-2 Blob options surface in `SnapshotBlobStorageOptions` and `SnapshotBlobDefaults`:
  - `BlobPrefix` with default `snapshots/`
  - `ListPageSizeHint` with default `500`
- Extended `SnapshotBlobStorageOptionsValidator` to enforce `ListPageSizeHint > 0`.
- Registered the new internal increment-2 runtime seams in `SnapshotBlobStorageProviderRegistrations`:
  - `IBlobNameStrategy` / `BlobNameStrategy`
  - `ISnapshotBlobOperations` / `SnapshotBlobOperations`
  - `ISnapshotBlobRepository` / `SnapshotBlobRepository`
- Added internal naming primitives under `src/Tributary.Runtime.Storage.Blob/Naming/`:
  - Canonical stream identity generation independent from `SnapshotStreamKey.ToString()` using a fixed-order JSON representation.
  - Hash-based stream prefixes derived from `SHA-256(canonicalStreamIdentity)`.
  - Zero-padded blob naming with the form `{normalizedBlobPrefix}{streamHash}/v{version:D20}.snapshot`.
  - Stream-scoped version parsing that rejects names from other streams.
- Added internal storage primitives under `src/Tributary.Runtime.Storage.Blob/Storage/`:
  - `SnapshotBlobOperations` uses Azure Blob conditional upload semantics with `If-None-Match = *` and translates `409/412` into duplicate-create failure.
  - `SnapshotBlobOperations.ListByPrefixAsync` enumerates blob names page by page without payload download.
  - `SnapshotBlobRepository.WriteIfAbsentAsync` converts duplicate-create failure into the internal `SnapshotBlobDuplicateVersionException`.
  - `SnapshotBlobRepository.ListVersionsAsync` and `GetLatestVersionAsync` operate on parsed blob names only, preserving stream-local paging behavior.
- Kept low-level naming and Blob SDK interaction internal to `Tributary.Runtime.Storage.Blob`.
- Kept full repository behavior, stored frame logic, checksum logic, and payload round-trip behavior out of scope for this increment.

## Tests Added Or Updated

- Updated `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageOptionsTests.cs` for the restored increment-2 options and validation rules.
- Added `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobNameStrategyTests.cs` covering:
  - exact canonical JSON identity contract for a representative `SnapshotStreamKey`
  - exact hashed stream-prefix contract for a representative stream
  - exact full blob-name contract for a representative `SnapshotKey`
  - lexical ordering traps (`9`, `10`, `11`, `100`)
  - stream-scoped version parsing
- Added `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobOperationsTests.cs` covering the Azure seam directly:
  - successful conditional create with `If-None-Match = *`
  - duplicate-create mapping for Azure `409` and `412`
  - prefix forwarding into `GetBlobsAsync`
  - page-size forwarding into `AsPages(..., pageSizeHint)`
  - page projection from Azure `BlobItem` results into `SnapshotBlobPage`
- Added `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobRepositoryTests.cs` covering:
  - successful conditional create
  - duplicate-version conflict mapping
  - stream-local paged listing
  - cross-stream isolation
  - latest-version selection across multiple pages
- Added `tests/Tributary.Runtime.Storage.Blob.L0Tests/StubSnapshotBlobOperations.cs` as the L0 test double for the new Blob operations seam.

## Validation Evidence

- Commands run:

```powershell
dotnet build .\src\Tributary.Runtime.Storage.Blob\Tributary.Runtime.Storage.Blob.csproj -c Release -warnaserror
dotnet test .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release -warnaserror
```

- Results:
  - Blob provider project build succeeded with zero warnings.
  - Blob L0 test project passed with zero warnings.
  - Test summary: 30 passed, 0 failed, 0 skipped.

## Risks And Mitigations

- Canonical stream identity currently uses a fixed-order JSON string. That is deterministic and independent from `ToString()`, and the increment-2 suite now pins one representative canonical JSON string, hashed stream prefix, and full blob name so future path-shape drift fails fast.
- Duplicate-version behavior is internal only in this increment via `SnapshotBlobDuplicateVersionException`. The public provider surface still does not expose full write behavior until increment 4.
- Stream-local listing relies on the hashed prefix and name parsing only. The Azure seam now has direct tests proving prefix forwarding, requested page-size forwarding, and page projection behavior, but header-level stream identity validation remains deferred until increment 3 introduces the stored frame.

## Follow-On Work For Increment 3

- Implement the stored Blob frame with magic/prelude, frame version, bounded header, and payload segment.
- Persist and validate canonical stream identity inside the frame header.
- Persist serializer identity, compression mode, payload size metadata, and checksum.
- Add fail-fast decode behavior for unknown frame version, unknown serializer id, unknown compression mode, checksum mismatch, and malformed payload/header cases.
- Produce the large-payload evidence artifact required by the final plan.