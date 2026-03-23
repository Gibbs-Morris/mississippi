# Branch Remediation Validation

Date: 2026-03-23
Branch: feature/tributary-blob-storage-provider
HEAD: 323199e07ed408b8cda50795f740a27d6421fbd5
Base: main @ c050e2a6280ede514bcf01c60b6dbb9ffd5a5db6

## Scope

This follow-up review validated only the previously reported merge-shaping blockers from `06-code-review/review-branch.md`:

- persisted serializer identity actually driving restore selection
- Crescent trust-slice flakiness from wall-clock polling
- stale state metadata
- ADR-0003 alignment

## Validation Performed

- Compared the current branch against `main...HEAD` with `git diff --name-only --find-renames`.
- Re-read the current runtime, ADR, and trust-slice sources tied to the blocker set.
- Ran `dotnet test .\tests\Tributary.Runtime.L0Tests\Tributary.Runtime.L0Tests.csproj -c Release --filter FullyQualifiedName~SnapshotStateConverterTests`.
- Ran `dotnet test .\samples\Crescent\Crescent.L2Tests\Crescent.L2Tests.csproj -c Release --filter FullyQualifiedName~BlobSnapshotTrustSliceTests`.
- Ran the same Crescent trust-slice test a second time with `--no-build`.

## Blocker Status

### 1. Persisted serializer identity driving restore selection

Status: RESOLVED

Evidence:

- `src/Tributary.Runtime.Storage.Blob/Storage/BlobEnvelopeCodec.cs:179` now carries the persisted `PayloadSerializerId` into the decoded `SnapshotEnvelope`, and `src/Tributary.Runtime.Storage.Blob/Storage/BlobEnvelopeCodec.cs:203` still persists the concrete serializer identity into the stored frame.
- `src/Tributary.Runtime/SnapshotStateConverter.cs:55`, `src/Tributary.Runtime/SnapshotStateConverter.cs:61`, and `src/Tributary.Runtime/SnapshotStateConverter.cs:92` now resolve the deserializer from `envelope.PayloadSerializerId` instead of always using the ambient default provider.
- `src/Tributary.Runtime/SnapshotStateConverter.cs:109` and `src/Tributary.Runtime/SnapshotStateConverter.cs:111` fail closed when the persisted serializer identity is missing or ambiguous.
- `tests/Tributary.Runtime.L0Tests/SnapshotStateConverterTests.cs:62`, `tests/Tributary.Runtime.L0Tests/SnapshotStateConverterTests.cs:83`, and `tests/Tributary.Runtime.L0Tests/SnapshotStateConverterTests.cs:99` now pin both the positive and unknown-identity restore paths.
- The focused `SnapshotStateConverterTests` run passed: 7 tests, 0 failures.

Conclusion:

The original restore-path defect is closed.

### 2. Crescent trust-slice flakiness from wall-clock polling

Status: NOT RESOLVED

### 3. Stale state metadata

Status: RESOLVED

Evidence:

- `.thinking/2026-03-23-tributary-blob-storage-provider/state.json:8` now records `lastCommitSha` as `323199e07ed408b8cda50795f740a27d6421fbd5`.
- `.thinking/2026-03-23-tributary-blob-storage-provider/state.json:9` now records `lastCommitTime` as `2026-03-23T05:24:58Z`.
- Those values match the current branch HEAD reviewed in this pass.

Conclusion:

The specific stale `state.json` mismatch called out in the original review is closed. The activity log still ends at the original branch review entry, but I do not consider that a remaining material blocker for this set now that the authoritative state artifact is synchronized.

### 4. ADR-0003 alignment

Status: RESOLVED

Evidence:

- `docs/Docusaurus/docs/adr/0003-persist-concrete-payload-serializer-identity-for-snapshot-bytes.md:42` says restart and reload do not depend on ambient serializer defaults.
- `docs/Docusaurus/docs/adr/0003-persist-concrete-payload-serializer-identity-for-snapshot-bytes.md:50` says readers reject unknown serializer identifiers without silently falling back to ambient defaults.
- The current runtime behavior in `src/Tributary.Runtime/SnapshotStateConverter.cs:55`, `src/Tributary.Runtime/SnapshotStateConverter.cs:61`, `src/Tributary.Runtime/SnapshotStateConverter.cs:92`, `src/Tributary.Runtime/SnapshotStateConverter.cs:109`, and `src/Tributary.Runtime/SnapshotStateConverter.cs:111` now matches those ADR claims.

Conclusion:

The ADR is now aligned with the implemented restore behavior.

## Remaining Material Issue

### 1. MAJOR | Reliability | `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs:29,31,69,209,216-217,240`

What is wrong:

The trust-slice still defines completion by a one-minute wall-clock timeout plus a `PeriodicTimer` polling loop over blob listing, so the end-to-end proof remains timing-sensitive instead of being driven by a deterministic readiness signal.

Evidence:

- `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs:29` and `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs:31` hard-code `BlobWriteTimeout` and `ProbeInterval`.
- `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs:69` gates the main assertion path on `WaitForSnapshotBlobAsync(...)`.
- `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs:209` through `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs:240` repeatedly list blobs until one appears or the timeout expires.
- The helper throws `"No snapshot blob was written to container '{containerName}' within the timeout."` on timing failure rather than proving completion through a domain or storage-level completion signal.
- I was able to get two consecutive passing runs of the trust slice in this validation pass, but successful runs do not remove the structural timing dependency that caused the original blocker.

Why it still matters:

This branch uses the Crescent trust slice as its end-to-end proof that Blob snapshot persistence and restart hydration work. As long as that proof depends on real-time polling against emulator-backed services, failures can still be dominated by timing variance instead of product regressions, which weakens merge confidence for the highest-value scenario in the branch.

Minimal fix:

Replace the polling loop with a deterministic readiness signal. The safest shape is to have the scenario or production seam expose the exact snapshot blob name or a completion signal after the write path commits, then inspect that known blob directly. If that is not practical, centralize the wait into a reusable helper that records richer diagnostics and is keyed to a known blob path rather than open-ended container listing.

How to verify:

- Remove `WaitForSnapshotBlobAsync` as the trust-slice completion mechanism.
- Re-run `BlobSnapshotTrustSliceTests` repeatedly, ideally at least 5 back-to-back runs, and confirm the test no longer depends on polling for eventual blob discovery.

## Conclusion

Three of the four original blockers are now closed:

- persisted serializer identity restore selection: resolved
- stale state metadata: resolved
- ADR-0003 alignment: resolved

One blocker remains open:

- Crescent trust-slice reliability still depends on wall-clock polling