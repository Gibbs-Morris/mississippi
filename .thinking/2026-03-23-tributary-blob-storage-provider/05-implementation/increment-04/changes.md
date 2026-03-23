# Increment 4 Changes

## Implementation Summary

- Replaced the increment-3 Blob provider placeholder with a real `ISnapshotStorageProvider` implementation in `SnapshotBlobStorageProvider`.
- Expanded the Blob provider logger surface to cover read, write, delete, delete-all, and prune operations through `SnapshotBlobStorageProviderLoggerExtensions`.
- Extended the low-level Blob SDK seam in `ISnapshotBlobOperations` and `SnapshotBlobOperations` with:
  - exact-name download via `DownloadIfExistsAsync`
  - exact-name idempotent delete via `DeleteIfExistsAsync`
- Expanded `ISnapshotBlobRepository` and `SnapshotBlobRepository` from increment-2 naming/listing primitives into full repository behavior:
  - codec-backed exact write via `WriteAsync`
  - codec-backed exact read via `ReadAsync`
  - internal latest-read via `ReadLatestAsync`
  - exact delete via `DeleteAsync`
  - delete-all via stream-local page listing plus exact-name delete
  - prune via a two-pass stream-local listing strategy that always retains the latest version plus versions matching non-zero retain moduli
- Integrated provider-wide `Off` and `Gzip` compression end to end by routing repository writes and reads through the existing `IBlobEnvelopeCodec`.
- Preserved duplicate-version conflict behavior by keeping writes on conditional-create semantics and surfacing `SnapshotBlobDuplicateVersionException`.
- Kept maintenance paths payload-light:
  - latest-read lists names to select the max version, then downloads exactly one selected blob body
  - delete-all lists names and deletes exact blob names without downloading bodies
  - prune performs list-only selection and exact-name deletes without downloading candidate bodies

## Tests Added Or Updated

- Updated `tests/Tributary.Runtime.Storage.Blob.L0Tests/StubSnapshotBlobOperations.cs` into an in-memory Blob seam that records creates, downloads, deletes, listings, and stored frame bytes.
- Expanded `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobRepositoryTests.cs` with increment-4 behavior coverage for:
  - missing exact read returns `null`
  - missing latest read returns `null`
  - exact write/read round-trip for both `Off` and `Gzip`
  - latest-read selects the numerically latest version and downloads only the selected blob body
  - delete-all remains stream-scoped and performs zero body downloads
  - prune retains modulus matches plus the latest version and performs zero body downloads
  - non-default serializer identities round-trip through the real repository path
- Added `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderTests.cs` covering provider-level integration for:
  - missing read returns `null`
  - write/read round-trip for both `Off` and `Gzip`
  - delete-missing remains idempotent and non-throwing
- Expanded `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobOperationsTests.cs` to cover:
  - exact-name blob download success
  - exact-name blob download miss
  - exact-name delete forwarding and result propagation

## Verification Evidence

- Commands run:

```powershell
dotnet build .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release -warnaserror
dotnet test .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release -warnaserror
```

- Results:
  - Blob provider and Blob L0 tests build succeeded with zero warnings.
  - Blob L0 test suite succeeded: 68 passed, 0 failed, 0 skipped.
  - Maintenance-path proof is covered at L0 by call-count assertions showing:
    - latest-read downloads exactly one selected blob body
    - delete-all downloads zero blob bodies
    - prune downloads zero blob bodies

## Risks And Mitigations

- Exact reads still whole-buffer the selected blob body before decode because the current codec contract accepts `ReadOnlyMemory<byte>`. This is limited to the selected blob only; non-selected candidates remain body-free.
- Prune uses a two-pass name listing to keep memory bounded and avoid materializing the full stream. This trades one extra prefix scan for simpler bounded-memory behavior.
- Provider-level metrics and richer failure diagnostics remain out of scope for this increment and are deferred to increment 5.

## Blockers

- None for increment 4.

## Follow-On Work For Increment 5

- Implement `CreateIfMissing` and `ValidateExists` initialization behavior end-to-end with failure-path coverage at the provider boundary.
- Add actionable diagnostics for duplicate conflicts, unreadable blobs, and startup misconfiguration outcomes.
- Verify the increment-5 observability matrix through focused L0 coverage.