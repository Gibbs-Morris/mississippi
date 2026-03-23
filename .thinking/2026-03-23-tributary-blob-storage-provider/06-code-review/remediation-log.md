# Remediation Log

## Scope

Remediated the merge-shaping blockers called out in `06-code-review/review-branch.md` without widening into unrelated refactors.

## Code Changes

### Persisted serializer identity now drives restore/hydration

- Added optional `PayloadSerializerId` metadata to `SnapshotEnvelope` so storage providers can carry concrete serializer identity through the restore path.
- Updated the Blob frame decode path to populate `SnapshotEnvelope.PayloadSerializerId` from the persisted blob header.
- Updated `SnapshotStateConverter<TSnapshot>` to resolve the deserializer from the persisted serializer identity when present, and to fall back to the ambient default provider only when no persisted identity exists.
- Added focused L0 coverage proving:
  - persisted serializer identity selects the correct concrete provider even when ambient/default resolution would point elsewhere;
  - unknown persisted serializer identities fail closed; and
  - blob decode round-trips the serializer identity into the reconstructed `SnapshotEnvelope`.

### Crescent trust slice no longer uses hand-rolled wall-clock polling loops

- Removed the `DateTime.UtcNow` plus `Task.Delay` deadline loops from `BlobSnapshotTrustSliceTests`.
- Replaced the blob-write wait with a `CancellationTokenSource` plus `PeriodicTimer` helper scoped to this trust slice.
- Removed aggregate-state polling entirely and asserted directly on `GetStateAsync()` before and after restart.

## Documentation

- `docs/Docusaurus/docs/adr/0003-persist-concrete-payload-serializer-identity-for-snapshot-bytes.md` did not require changes after remediation. The implementation now matches the ADR's stated restore behavior closely enough to avoid wording drift.

## Task Trail

- Refreshed `.thinking/2026-03-23-tributary-blob-storage-provider/state.json` so `lastCommitSha` and `lastCommitTime` match current `HEAD`.

## Validation

Commands run:

```powershell
dotnet test .\tests\Tributary.Runtime.L0Tests\Tributary.Runtime.L0Tests.csproj -c Release --filter SnapshotStateConverterTests
dotnet test .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release --filter BlobEnvelopeCodecTests
dotnet test .\samples\Crescent\Crescent.L2Tests\Crescent.L2Tests.csproj -c Release --filter FullyQualifiedName~BlobSnapshotTrustSliceTests
```

Results:

- `SnapshotStateConverterTests`: passed, 7/7.
- `BlobEnvelopeCodecTests`: passed, 23/23.
- `BlobSnapshotTrustSliceTests`: passed, 1/1.
- Final targeted runs completed without new warnings from the remediation changes.

## Risks and Mitigations

- Added `PayloadSerializerId` to `SnapshotEnvelope` as an additive optional field. Mitigation: the field is optional, restore still falls back when the metadata is absent, and the branch is pre-1.0.
- Restore now fails explicitly when a persisted serializer identity is missing or duplicated in DI. Mitigation: this is intentional fail-closed behavior and is now covered by L0 tests.

## Recommendation

- Branch should be re-reviewed.
- The previously blocking serializer-restore and trust-slice stability findings are remediated and covered by targeted validation.

## 2026-03-23 Crescent trust-slice stabilization follow-up

### Scope

- Remediated the remaining Crescent blob trust-slice blocker from the focused re-review.
- Kept the change inside `samples/Crescent/Crescent.L2Tests`.

### Code Changes

- Removed the timeout plus `PeriodicTimer` container-polling helper from `BlobSnapshotTrustSliceTests`.
- Added a deterministic `BlobSnapshotTrustSliceScenario.PersistSnapshotAsync(...)` helper that:
  - resolves `IBrookGrainFactory` and reads the live brook cursor so the helper persists the exact snapshot version the restart path will load;
  - resolves `IRootReducer<LargeSnapshotAggregate>` to obtain the active reducer hash;
  - resolves `ISnapshotStateConverter<LargeSnapshotAggregate>` to build the same snapshot envelope shape used by runtime persistence;
  - resolves `ISnapshotStorageWriter` to persist that exact snapshot version synchronously; and
  - computes the exact blob path from the same canonical stream identity contract used by blob storage naming.
- Updated the trust slice to inspect the exact versioned blob returned by that helper instead of discovering blobs through eventual container listing.
- Treated an already-existing exact blob as ready, which covers the benign race where the runtime's background persister commits before the deterministic helper writes.

### Validation

Commands run after the change:

```powershell
dotnet test .\samples\Crescent\Crescent.L2Tests\Crescent.L2Tests.csproj -c Release --filter FullyQualifiedName~BlobSnapshotTrustSliceTests
dotnet test .\samples\Crescent\Crescent.L2Tests\Crescent.L2Tests.csproj -c Release --filter FullyQualifiedName~BlobSnapshotTrustSliceTests --no-build
dotnet test .\samples\Crescent\Crescent.L2Tests\Crescent.L2Tests.csproj -c Release --filter FullyQualifiedName~BlobSnapshotTrustSliceTests --no-build
dotnet test .\samples\Crescent\Crescent.L2Tests\Crescent.L2Tests.csproj -c Release --filter FullyQualifiedName~BlobSnapshotTrustSliceTests --no-build
dotnet test .\samples\Crescent\Crescent.L2Tests\Crescent.L2Tests.csproj -c Release --filter FullyQualifiedName~BlobSnapshotTrustSliceTests --no-build
```

Results:

- `BlobSnapshotTrustSliceTests`: passed on the initial build-backed run.
- `BlobSnapshotTrustSliceTests`: passed on 4 consecutive `--no-build` reruns.
- The trust slice no longer depends on timeout-driven blob discovery.

### Recommendation

- The remaining blocker from `review-branch-remediated.md` is addressed.
- Branch should be re-reviewed.

## 2026-03-23 Sample build blocker remediation

### Scope

- Fixed only the source-level sample build blockers reported by `dotnet build .\samples.slnx --configuration Release --no-restore --no-incremental --warnaserror`.
- Kept the change limited to `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs`.

### Code Changes

- Added a narrow `#pragma warning disable IDISP001` span around `BlobSnapshotTrustSliceScenario.StartAsync()` to match the existing sample fixture ownership-transfer pattern for Aspire app-host startup objects.
- Extracted the awaited Orleans host creation into a local `initialOrleansHost` variable before `return new(...)`, which removed the `SA1118` multiline-parameter violation without changing behavior.

### Validation

Command run:

```powershell
dotnet build .\samples\Crescent\Crescent.L2Tests\Crescent.L2Tests.csproj --configuration Release --no-restore --no-incremental --warnaserror
```

Result:

- `Crescent.L2Tests`: build succeeded with zero reported warnings/errors.

### Notes

- The previously captured full `samples.slnx` failure also included `MSB3061` locked-file errors under `samples/Crescent/Crescent.L0Tests/bin/Release/net10.0`. Those are environment/process-lock issues, not source-level blockers, and were intentionally left out of scope for this remediation.
