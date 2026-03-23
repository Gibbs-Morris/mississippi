# Commit Review

Verdict: acceptable to commit as a small focused increment.

Remaining material issues: none.

## Scope

- Review target: current uncommitted working-tree changes for increment 3
- Review focus: prior blockers only
- Current branch: `feature/tributary-blob-storage-provider`
- HEAD SHA: `57cb87d0777c28bf29b1a715213695c765c808a0`
- Base branch: `main`
- Base SHA: `c050e2a6280ede514bcf01c60b6dbb9ffd5a5db6`

## Coverage Evidence

Changed-file count: 12

Deterministic checklist:

- REVIEWED `.thinking/2026-03-23-tributary-blob-storage-provider/activity-log.md`
- REVIEWED `.thinking/2026-03-23-tributary-blob-storage-provider/state.json`
- REVIEWED `src/Tributary.Runtime.Storage.Blob/SnapshotBlobDefaults.cs`
- REVIEWED `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageOptions.cs`
- REVIEWED `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProviderRegistrations.cs`
- REVIEWED `src/Tributary.Runtime.Storage.Blob/Startup/SnapshotBlobStorageOptionsValidator.cs`
- REVIEWED `src/Tributary.Runtime.Storage.Blob/Startup/SnapshotPayloadSerializerResolver.cs`
- REVIEWED `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageOptionsTests.cs`
- REVIEWED `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderRegistrationsTests.cs`
- REVIEWED `tests/Tributary.Runtime.Storage.Blob.L0Tests/TestSerializationProvider.cs`
- REVIEWED `tests/Tributary.Runtime.Storage.Blob.L0Tests/Tributary.Runtime.Storage.Blob.L0Tests.csproj`
- REVIEWED `tests/Tributary.Runtime.Storage.Blob.L0Tests/packages.lock.json`

Cross-file evidence read to validate the two blockers:

- `src/Tributary.Runtime.Storage.Blob/Storage/BlobEnvelopeCodec.cs`
- `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobEnvelopeCodecTests.cs`
- `tests/Tributary.Runtime.Storage.Blob.L0Tests/AlternateTestSerializationProvider.cs`

## Blocker Validation

### Oversized-header decode handling

Status: resolved.

Evidence:

- `src/Tributary.Runtime.Storage.Blob/Storage/BlobEnvelopeCodec.cs:121` reads the persisted header length as `uint`.
- `src/Tributary.Runtime.Storage.Blob/Storage/BlobEnvelopeCodec.cs:123` rejects values above `int.MaxValue` before any cast.
- `src/Tributary.Runtime.Storage.Blob/Storage/BlobEnvelopeCodec.cs:124` rejects values above the configured `MaxHeaderBytes`.
- `src/Tributary.Runtime.Storage.Blob/Storage/BlobEnvelopeCodec.cs:129` routes these cases through `SnapshotBlobUnreadableFrameReason.InvalidHeaderLength`.
- `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobEnvelopeCodecTests.cs:188` directly asserts the configured-maximum case fails closed with `InvalidHeaderLength`.
- `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobEnvelopeCodecTests.cs:205` directly asserts the `uint.MaxValue` overflow case fails closed with `InvalidHeaderLength`.

Conclusion: the prior `OverflowException` escape hatch is closed.

### Unreadable-frame claims versus direct test coverage

Status: aligned.

Evidence:

- `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobEnvelopeCodecTests.cs:155` directly covers `TruncatedPrelude`.
- `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobEnvelopeCodecTests.cs:188` directly covers configured-max header rejection.
- `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobEnvelopeCodecTests.cs:205` directly covers the overflowing header-length case.
- `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobEnvelopeCodecTests.cs:239` directly covers `InvalidHeaderValues`.
- `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobEnvelopeCodecTests.cs:292` directly covers `InvalidStoredPayloadLength`.
- `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobEnvelopeCodecTests.cs:328` directly covers `InvalidUncompressedPayloadLength`.

Conclusion: the branch now has dedicated direct codec tests for the unreadable-frame scenarios that mattered to the prior review note. The current uncommitted slice does not reintroduce any mismatch.

## Validation Run

- `dotnet test .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release -warnaserror --filter "FullyQualifiedName~BlobEnvelopeCodecTests|FullyQualifiedName~SnapshotBlobStorageOptionsTests|FullyQualifiedName~SnapshotBlobStorageProviderRegistrationsTests" --logger "console;verbosity=minimal"`
- Result: passed with 36 tests, 0 failures, 0 warnings.

## Recommendation

This current uncommitted slice is commit-safe for increment 3.

- The oversized-header decode blocker is fixed at the codec root cause and pinned by direct tests.
- The unreadable-frame coverage concern no longer stands; the direct codec tests now cover the previously disputed cases.
- The changed files remain small and focused on wiring, validation, and test support for the codec behavior already present in the branch.