# Branch Review: feature/tributary-blob-storage-provider vs main

## 1) Initial draft

Requirements and constraints:

- Review every changed file in `main...HEAD` for `feature/tributary-blob-storage-provider`.
- Report findings only. No code changes, no PR comments, no GitHub MCP usage.
- Anchor every finding to file path and line numbers.
- Write the review into this file.

Branch metadata:

- Current branch: `feature/tributary-blob-storage-provider`
- HEAD SHA: `323199e07ed408b8cda50795f740a27d6421fbd5`
- Base branch: `main`
- Base SHA: `c050e2a6280ede514bcf01c60b6dbb9ffd5a5db6`

Initial plan:

1. Compute the complete changed-file list from `main...HEAD`, including renames/adds/deletes.
2. Build a deterministic alphabetical review checklist.
3. Review each changed file one-by-one by reading the full file at HEAD and the branch diff intent.
4. Record findings with severity, category, evidence, fix, and verification guidance.
5. Produce a final report with coverage evidence, totals, and top risks.

Assumptions and unknowns:

- The local `main` ref is the intended review baseline.
- `.thinking` artifacts are in scope because they are part of the branch diff.
- The repeated Crescent L2 test failures seen in local terminal history may reflect real flakiness, but they still need source-level confirmation before being called out.

Claim list:

- Every changed file was reviewed.
- Every finding below includes file path and line references.
- Files with no findings are still listed in the checklist and marked `REVIEWED`.

## 2) Verification questions

1. Is the changed-file list complete, including adds/renames/deletes?
2. Was the review order deterministic and alphabetical?
3. Are the line references anchored to current branch contents?
4. For each substantive bug claim, is there corroborating evidence beyond a single line?
5. Were cross-file contract changes checked, especially serializer selection, persisted blob framing, and DI registration behavior?
6. Are there missing tests for the changed runtime behavior?
7. Are persistence-format and serializer-compatibility risks evaluated under the repo's pre-1.0 policy?
8. Do the docs and ADRs match the actual implementation behavior?
9. Do the `.thinking` artifacts remain internally consistent enough to trust as review evidence?

## 3) Independent answers (evidence-based)

1. Yes. `git diff --name-status --find-renames main...HEAD` and `git diff --name-only --find-renames main...HEAD` show 120 changed files and no renames/deletes.
2. Yes. The checklist below follows the exact alphabetical order returned by `git diff --name-only --find-renames main...HEAD`.
3. Yes. Findings are anchored to HEAD file reads and line-number searches against the working tree.
4. Yes for the main issues:
   - Serializer-identity finding is supported by `BlobEnvelopeCodec`, `SnapshotStateConverter`, `SnapshotCacheGrain`, the Crescent L2 scenario wiring, ADR-0003, and the current L0/L2 tests.
   - Flaky-test finding is supported by the trust-slice source itself and by repeated local failures already present in terminal history.
   - Stale-artifact finding is supported by `state.json`, `activity-log.md`, and the actual branch HEAD SHA.
5. Yes. The review checked the new Blob storage provider stack, sample trust slice, test projects, solution wiring, and ADRs.
6. Yes. The branch is missing a test that proves restore chooses the persisted serializer identity rather than whichever `ISerializationProvider` DI happens to return.
7. Yes. API breaks are acceptable pre-1.0, but persisted blob format and blob readability guarantees are still important and are explicitly claimed by the ADRs.
8. No. ADR-0003 currently overclaims restart independence from ambient serializer defaults.
9. Mostly, but `state.json` is stale relative to the last commit/activity-log entries, so the task trail is not fully synchronized.

## 4) Final revised plan

Revised execution plan:

1. Use the 120-file `main...HEAD` list as the authoritative checklist.
2. Keep the exact alphabetical order from the diff output.
3. Record only merge-shaping findings, not file-by-file noise.
4. Call out residual risks where the branch otherwise looks acceptable.

Reporting format:

- Coverage evidence
- Review checklist with per-file `REVIEWED` status
- Issue report grouped by file
- Totals by severity and category
- Top risks and next actions

## 5) Review

### Coverage evidence

Changed-file count: `120`

Checklist status legend: `REVIEWED`

- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/00-intake.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/01-discovery/gap-analysis-round-01.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/01-discovery/gap-analysis-round-02.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/01-discovery/questions-round-01.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/01-discovery/questions-round-02.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/01-discovery/questions-round-03.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/01-discovery/requirements-synthesis.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/02-three-amigos/adoption-perspective.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/02-three-amigos/business-perspective.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/02-three-amigos/qa-perspective.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/02-three-amigos/synthesis.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/02-three-amigos/technical-perspective.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/03-architecture/adr-notes.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/03-architecture/c4-component.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/03-architecture/c4-container.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/03-architecture/c4-context.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/03-architecture/expert-cloud-review.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/03-architecture/expert-serialization-review.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/03-architecture/solution-design.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/draft-plan-v1.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/draft-plan-v2.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/final-plan.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-01/review-developer-evangelist.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-01/review-dx.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-01/review-performance.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-01/review-qa-lead.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-01/review-solution-architect.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-01/review-tech-lead.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-01/synthesis.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-02/review-developer-evangelist.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-02/review-dx.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-02/review-performance.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-02/review-qa-lead.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-02/review-tech-lead.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-02/synthesis.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-03/cosmos-behavior-evidence.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-03/review-developer-evangelist.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-03/review-dx.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-03/review-performance.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-03/review-qa-lead.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/04-planning/review-cycle-03/review-tech-lead.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-01/changes.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-01/commit-review.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-01/test-results.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-02/changes.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-02/commit-review.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-03/changes.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-03/commit-review.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-03/large-payload-evidence.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-04/changes.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-04/commit-review.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-05/changes.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-05/commit-review.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-06/changes.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-06/commit-review.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/activity-log.md`
- REVIEWED | `.thinking/2026-03-23-tributary-blob-storage-provider/state.json`
- REVIEWED | `docs/Docusaurus/docs/adr/0001-use-canonical-stream-identity-and-hashed-blob-naming-for-snapshot-blobs.md`
- REVIEWED | `docs/Docusaurus/docs/adr/0002-store-snapshots-in-a-versioned-self-describing-blob-frame.md`
- REVIEWED | `docs/Docusaurus/docs/adr/0003-persist-concrete-payload-serializer-identity-for-snapshot-bytes.md`
- REVIEWED | `docs/Docusaurus/docs/adr/0004-compress-only-snapshot-payload-bytes-and-verify-payload-integrity.md`
- REVIEWED | `docs/Docusaurus/docs/adr/0005-use-conditional-blob-creation-and-stream-local-maintenance-scans.md`
- REVIEWED | `docs/Docusaurus/docs/adr/0006-use-configurable-container-initialization-for-blob-snapshot-storage.md`
- REVIEWED | `mississippi.slnx`
- REVIEWED | `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs`
- REVIEWED | `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs`
- REVIEWED | `samples/Crescent/Crescent.L2Tests/Crescent.L2Tests.csproj`
- REVIEWED | `samples/Crescent/Crescent.L2Tests/CrescentBlobCustomJsonSerializationProvider.cs`
- REVIEWED | `samples/Crescent/Crescent.L2Tests/LargeSnapshotAggregate.cs`
- REVIEWED | `samples/Crescent/Crescent.L2Tests/LargeSnapshotScenarioRegistrations.cs`
- REVIEWED | `samples/Crescent/Crescent.L2Tests/LargeSnapshotStored.cs`
- REVIEWED | `samples/Crescent/Crescent.L2Tests/LargeSnapshotStoredEventReducer.cs`
- REVIEWED | `samples/Crescent/Crescent.L2Tests/StoreLargeSnapshotCommand.cs`
- REVIEWED | `samples/Crescent/Crescent.L2Tests/StoreLargeSnapshotCommandHandler.cs`
- REVIEWED | `samples/Crescent/Crescent.L2Tests/packages.lock.json`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Naming/BlobNameStrategy.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Naming/IBlobNameStrategy.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/SnapshotBlobCompression.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/SnapshotBlobContainerInitializationMode.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/SnapshotBlobDefaults.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageOptions.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProvider.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProviderLoggerExtensions.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProviderRegistrations.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Startup/BlobContainerInitializer.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Startup/BlobContainerInitializerOperations.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Startup/IBlobContainerInitializerOperations.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Startup/SnapshotBlobStartupLoggerExtensions.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Startup/SnapshotBlobStorageOptionsValidator.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Startup/SnapshotPayloadSerializerDescriptor.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Startup/SnapshotPayloadSerializerResolver.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Storage/BlobEnvelopeCodec.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Storage/DecodedSnapshotBlobFrame.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Storage/IBlobEnvelopeCodec.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Storage/ISnapshotBlobOperations.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Storage/ISnapshotBlobRepository.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Storage/SnapshotBlobDuplicateVersionException.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Storage/SnapshotBlobOperations.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Storage/SnapshotBlobPage.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Storage/SnapshotBlobRepository.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Storage/SnapshotBlobUnreadableFrameException.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Storage/SnapshotBlobUnreadableFrameReason.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Storage/StoredSnapshotBlobHeader.cs`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/Tributary.Runtime.Storage.Blob.csproj`
- REVIEWED | `src/Tributary.Runtime.Storage.Blob/packages.lock.json`
- REVIEWED | `tests/Tributary.Runtime.Storage.Blob.L0Tests/AlternateTestSerializationProvider.cs`
- REVIEWED | `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobEnvelopeCodecTests.cs`
- REVIEWED | `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobNameStrategyTests.cs`
- REVIEWED | `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobOperationsTests.cs`
- REVIEWED | `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobRepositoryTests.cs`
- REVIEWED | `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageOptionsTests.cs`
- REVIEWED | `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderRegistrationsTests.cs`
- REVIEWED | `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderTests.cs`
- REVIEWED | `tests/Tributary.Runtime.Storage.Blob.L0Tests/StubBlobContainerInitializerOperations.cs`
- REVIEWED | `tests/Tributary.Runtime.Storage.Blob.L0Tests/StubSnapshotBlobOperations.cs`
- REVIEWED | `tests/Tributary.Runtime.Storage.Blob.L0Tests/TestLogEntry.cs`
- REVIEWED | `tests/Tributary.Runtime.Storage.Blob.L0Tests/TestLogger.cs`
- REVIEWED | `tests/Tributary.Runtime.Storage.Blob.L0Tests/TestSerializationProvider.cs`
- REVIEWED | `tests/Tributary.Runtime.Storage.Blob.L0Tests/Tributary.Runtime.Storage.Blob.L0Tests.csproj`
- REVIEWED | `tests/Tributary.Runtime.Storage.Blob.L0Tests/packages.lock.json`

All 120 changed files reached `REVIEWED`.

### Issue report

#### `src/Tributary.Runtime.Storage.Blob/Storage/BlobEnvelopeCodec.cs`

1. `MAJOR` | `Correctness`

What’s wrong:

Persisted serializer identity is written into the blob frame and validated on read, but it is never used to choose the deserializer that hydrates the snapshot state.

Evidence:

- `BlobEnvelopeCodec.cs:138` only checks that `header.PayloadSerializerId` exists in `KnownPayloadSerializerIds`.
- `BlobEnvelopeCodec.cs:177` returns a plain `SnapshotEnvelope` with `DataContentType = header.DataContentType`, but does not carry a selected serializer instance or identity forward.
- `BlobEnvelopeCodec.cs:202` writes `PayloadSerializerId = payloadSerializerDescriptor.SerializerId`, so the provider claims to persist concrete serializer identity.
- `SnapshotStateConverter.cs:31` and `SnapshotStateConverter.cs:37` deserialize with the ambient injected `ISerializationProvider`, not the persisted serializer identity.
- `SnapshotCacheGrain.cs:155` uses `SnapshotStateConverter.FromEnvelope(envelope)` during restore, so runtime hydration follows the ambient provider path.
- The new Crescent trust slice wires both the default JSON provider and the custom provider (`BlobSnapshotTrustSliceScenario.cs:190-191`) and sets `PayloadSerializerFormat` separately (`BlobSnapshotTrustSliceScenario.cs:230`), which means the happy path currently depends on DI resolution order rather than the stored blob’s serializer metadata.

Why it’s wrong:

ADR-0003 explicitly promises that restart/reload should not depend on ambient serializer defaults. The current implementation only rejects unknown serializer IDs; it does not actually use the persisted serializer identity to restore snapshot state. This means a registration-order change, a changed default provider, or two providers that can both deserialize the same content family can break restore despite the blob being self-describing.

Fix:

Make restore select the deserializer from persisted blob metadata rather than ambient `ISerializationProvider` resolution. The minimum safe shape is to propagate the resolved serializer identity/provider through the read path and have snapshot hydration use that provider, or else remove the concrete-identity claim and simplify the contract.

How to verify:

- Add an L0 or L1 test that persists a snapshot with one concrete provider, restarts with both providers registered in the opposite order, and proves restore still uses the persisted serializer identity.
- Re-run the Crescent trust slice with the provider registration order reversed; it should still pass if the implementation is correct.

#### `docs/Docusaurus/docs/adr/0003-persist-concrete-payload-serializer-identity-for-snapshot-bytes.md`

1. `MINOR` | `Maintainability`

What’s wrong:

ADR-0003 documents guarantees that the implementation does not currently meet.

Evidence:

- `0003-persist-concrete-payload-serializer-identity-for-snapshot-bytes.md:42` says restart and reload do not depend on ambient serializer defaults.
- `0003-persist-concrete-payload-serializer-identity-for-snapshot-bytes.md:50` says readers reject unknown serializer identifiers without silently falling back to ambient defaults.
- The implementation still hydrates through the ambient `ISerializationProvider` in `SnapshotStateConverter.cs:37`.

Why it’s wrong:

This ADR is acting as a design contract for the new persisted format. Keeping it ahead of reality makes future reviews think the compatibility problem is solved when it is not.

Fix:

Either implement the ADR fully or narrow the ADR wording so it accurately describes the current behavior and remaining work.

How to verify:

- Compare the final runtime restore path against ADR-0003 after the serializer-selection fix.

#### `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs`

1. `MAJOR` | `Reliability`

What’s wrong:

The new trust-slice test is time-based and polling-based, which makes it inherently flaky and hard to diagnose.

Evidence:

- `BlobSnapshotTrustSliceTests.cs:212-224` polls aggregate state using `DateTime.UtcNow` plus `Task.Delay(200)`.
- `BlobSnapshotTrustSliceTests.cs:236-252` polls blob enumeration using `DateTime.UtcNow` plus `Task.Delay(200)`.
- Local terminal history already shows repeated failures for this exact test filter (`dotnet test .\samples\Crescent\Crescent.L2Tests\Crescent.L2Tests.csproj -c Release --filter FullyQualifiedName~BlobSnapshotTrustSliceTests`).

Why it’s wrong:

The branch’s only end-to-end proof for the new provider becomes unstable if completion is defined by real-time polling against emulator-backed services. That weakens merge confidence exactly where this branch is supposed to provide trust-slice evidence.

Fix:

Replace wall-clock polling with deterministic readiness signals where possible. At minimum, centralize retry logic with richer diagnostics, use bounded retry helpers, and avoid duplicated hand-rolled loops in the test itself.

How to verify:

- Run the trust slice in a loop, for example 10 consecutive times, and confirm it is stable.
- Capture timeout diagnostics that show whether failures are due to Orleans restore timing, blob visibility timing, or emulator startup timing.

#### `.thinking/2026-03-23-tributary-blob-storage-provider/state.json`

1. `MINOR` | `Maintainability`

What’s wrong:

The task-state artifact is stale relative to the actual branch state and its own activity log.

Evidence:

- `state.json:8-9` still records `lastCommitSha` as `f6d23e383f90d8380afd5b7ecfdb732f0399e7c1` and `lastCommitTime` as `2026-03-23T02:15:00Z`.
- `activity-log.md:63-64` records a later increment-6 commit (`323199e0`) and the transition into code review.
- Actual branch HEAD is `323199e07ed408b8cda50795f740a27d6421fbd5`.

Why it’s wrong:

If these `.thinking` artifacts are meant to be merge evidence, stale state metadata makes the branch trail harder to trust.

Fix:

Either keep `state.json` synchronized with the activity log and branch HEAD, or stop committing it as authoritative state.

How to verify:

- Compare `state.json` to `git rev-parse HEAD` and to the final activity-log entries before merge.

### Totals

By severity:

- `MAJOR`: 2
- `MINOR`: 2
- `BLOCKER`: 0
- `NIT`: 0

By category:

- `Correctness`: 1
- `Reliability`: 1
- `Maintainability`: 2

### Top risks + next actions

1. Fix serializer selection first. The branch currently claims persisted serializer identity as a compatibility guarantee, but restore still depends on ambient DI behavior.
2. Stabilize the Crescent trust slice next. It is the branch’s primary end-to-end proof and is already showing failure signals locally.
3. Sync or downgrade the `.thinking` state artifact if it is going to remain in the branch; otherwise the review trail itself stays noisy and partially stale.

Residual risk after addressing the findings:

- The provider still intentionally uses stream-local scans for latest/prune/delete-all. That is a conscious v1 tradeoff rather than a correctness defect, but it remains the main operational scaling constraint for very high-version streams.