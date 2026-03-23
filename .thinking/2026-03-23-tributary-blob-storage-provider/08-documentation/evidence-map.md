# Documentation Evidence Map

## Source Evidence Used

- `src/Tributary.Runtime.Storage.Blob/*`
- `src/Tributary.Abstractions/SnapshotEnvelope.cs`
- `src/Tributary.Runtime/SnapshotStateConverter.cs`
- `tests/Tributary.Runtime.Storage.Blob.L0Tests/*`
- `tests/Tributary.Runtime.L0Tests/SnapshotStateConverterTests.cs`
- `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs`
- `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs`
- `docs/Docusaurus/docs/adr/0001-use-canonical-stream-identity-and-hashed-blob-naming-for-snapshot-blobs.md`
- `docs/Docusaurus/docs/adr/0002-store-snapshots-in-a-versioned-self-describing-blob-frame.md`
- `docs/Docusaurus/docs/adr/0003-persist-concrete-payload-serializer-identity-for-snapshot-bytes.md`
- `docs/Docusaurus/docs/adr/0004-compress-only-snapshot-payload-bytes-and-verify-payload-integrity.md`
- `docs/Docusaurus/docs/adr/0005-use-conditional-blob-creation-and-stream-local-maintenance-scans.md`
- `docs/Docusaurus/docs/adr/0006-use-configurable-container-initialization-for-blob-snapshot-storage.md`

## Claims Backed

- Blob provider package and registration paths
- Supported compression modes and default behavior
- Provider options and startup validation behavior
- Stored frame behavior and serializer-identity persistence
- Duplicate-write conflict semantics
- Unreadable-frame behavior and fail-closed restore path
- Stream-local scan behavior for latest-read, delete-all, and prune
- Crescent trust-slice proof of Blob-backed restart hydration

## Verification Commands Reported By Docs Specialist

- `git diff --name-status --find-renames main...HEAD`
- `git diff --name-only --find-renames main...HEAD`
- `pwsh ./run-docs.ps1 -Mode Build`

## Verification Outcome

- Docs build succeeded.
- No editor problems were reported for the changed Docusaurus pages.