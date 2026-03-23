# QA Test Strategy Review

Date: 2026-03-23
Branch: `feature/tributary-blob-storage-provider`
HEAD: `57f44a88bf7e2d890b61d80818a637106ae81738`
Base: `main` @ `c050e2a6280ede514bcf01c60b6dbb9ffd5a5db6`

## Overall Assessment

The branch is in materially better shape than the earlier branch-review snapshots suggested. The current HEAD has strong targeted L0 evidence for the Blob provider seams and for serializer-aware restore behavior, and the Crescent Blob trust slice now reproduces reliably enough to support the main happy-path claim.

The remaining QA concern is not obvious functional breakage. It is evidence breadth. The branch is well defended at the seam level, but real-infrastructure coverage is still narrow and the final repository-wide quality gates have not yet been re-proven on the current HEAD in this QA pass.

Current handoff position:

- `Targeted feature confidence`: high
- `Real-infrastructure breadth`: moderate
- `Final handoff readiness`: conditional on running the full repo quality gates

## Evidence Reviewed

### Current execution evidence

- `dotnet test .\tests\Tributary.Runtime.L0Tests\Tributary.Runtime.L0Tests.csproj -c Release --filter FullyQualifiedName~SnapshotStateConverterTests`
  - result: `7/7 passed`
- `dotnet test .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release`
  - result: `73/73 passed`
- `dotnet test .\samples\Crescent\Crescent.L2Tests\Crescent.L2Tests.csproj -c Release --filter FullyQualifiedName~BlobSnapshotTrustSliceTests`
  - result: `1/1 passed`
- `1..5 | ForEach-Object { dotnet test .\samples\Crescent\Crescent.L2Tests\Crescent.L2Tests.csproj -c Release --filter FullyQualifiedName~BlobSnapshotTrustSliceTests --no-build }`
  - result: `5/5 passed`

### Coverage shape observed in source

- Blob L0 suite covers naming, codec, Azure seam, repository behavior, provider behavior, startup validation, and DI registration.
  - `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobEnvelopeCodecTests.cs`
  - `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobNameStrategyTests.cs`
  - `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobOperationsTests.cs`
  - `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobRepositoryTests.cs`
  - `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageOptionsTests.cs`
  - `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderRegistrationsTests.cs`
  - `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderTests.cs`
- Serializer-aware restore is directly pinned in `tests/Tributary.Runtime.L0Tests/SnapshotStateConverterTests.cs`.
- The branch-specific real-environment proof is concentrated in one trust-slice test:
  - `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs:38`

### Historical evidence reviewed

- `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-03/large-payload-evidence.md`
- `.thinking/2026-03-23-tributary-blob-storage-provider/06-code-review/remediation-log.md`
- `.thinking/2026-03-23-tributary-blob-storage-provider/state.json`

## Quality Findings

### 1. Strong seam-level coverage, especially for the high-risk restore path

Assessment: positive

Why it matters:

- The highest-risk correctness defect found during branch review was persisted serializer identity not actually driving restore.
- That is now directly tested in `tests/Tributary.Runtime.L0Tests/SnapshotStateConverterTests.cs:61`, `:98`, and `:119` through explicit provider-selection and fail-closed cases.
- The Blob provider seam coverage is also well decomposed. Repository rules such as duplicate-version rejection, list-driven maintenance, and non-default serializer round-trip are pinned in `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobRepositoryTests.cs:28`, `:49`, `:162`, `:190`, `:212`, and `:267`.

QA interpretation:

- This is credible targeted coverage, not just incidental pass-through coverage.
- If a regression lands in naming, frame decoding, serializer resolution, or repository maintenance, L0 is likely to catch it quickly.

Residual risk:

- low

### 2. Real-infrastructure coverage is still too narrow for the full operational surface

Assessment: residual risk

Why it matters:

- The branch-specific L2 proof is effectively one scenario: `LargeSnapshotShouldSurviveRestartWithGzipAndNonDefaultSerializer` in `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs:38`.
- That scenario is valuable, but it is a happy-path trust slice. It proves canonical registration, gzip framing, blob inspection, manual overwrite, Orleans restart, and blob-backed reload.
- It does not prove the main failure modes against real Azurite-hosted storage:
  - duplicate-version conflict behavior at the product boundary
  - unreadable/corrupted blob handling through the real provider path
  - `ValidateExists` startup behavior against an actual missing container
  - delete-all and prune behavior against a real container namespace

Evidence:

- Those behaviors are covered in L0 through stubs and mocks:
  - `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderTests.cs:72` and `:88`
  - `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderRegistrationsTests.cs:150`, `:176`, `:216`, `:255`, `:294`, and `:346`
- They are not covered by additional branch-specific L2 tests under `samples/Crescent/Crescent.L2Tests`.

QA interpretation:

- The test strategy is good at isolating logic defects, but still thin on proving storage-operational behavior in the real environment.
- That is acceptable for a focused branch if acknowledged, but it should be called out before handoff rather than implied away.

Residual risk:

- medium

### 3. Large-payload evidence proves correctness, not worst-case storage behavior

Assessment: residual risk

Why it matters:

- The large-payload matrix in `.thinking/2026-03-23-tributary-blob-storage-provider/05-implementation/increment-03/large-payload-evidence.md` uses a deterministic repeating pattern and runs only with `gzip`.
- The recorded stored sizes are extremely small relative to input size because the payload is intentionally highly compressible.
- The L0 matrix in `tests/Tributary.Runtime.Storage.Blob.L0Tests/BlobEnvelopeCodecTests.cs:389-393` therefore proves frame correctness and bounded buffering, but it does not prove behavior under poor compression ratios or near-worst-case blob sizes.

QA interpretation:

- This is valid engineering evidence for codec correctness.
- It is not enough evidence to make a strong capacity or performance claim for production-like payloads.

Residual risk:

- medium

### 4. End-to-end evidence is now repeatable, but still expensive and environment-coupled

Assessment: residual risk

Why it matters:

- The current trust slice did pass one build-backed run plus five `--no-build` reruns on the current HEAD.
- That materially improves confidence compared with the earlier history of repeated local failures.
- But each iteration still boots Aspire resources and external emulators, and the scenario remains bounded by a `10` minute timeout in `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs:31` with health waits at `:99-103`.

QA interpretation:

- Reliability is much better than before, and I do not consider the trust slice blocked.
- The test is still costly enough that it should be treated as a confidence slice, not as the sole final acceptance gate.

Residual risk:

- low to medium

### 5. Final handoff evidence is incomplete against repository policy

Assessment: remaining handoff gap

Why it matters:

- Repository policy expects build, cleanup, unit tests, and Mississippi mutation testing before work is called complete.
- This QA pass validated targeted tests on the current HEAD, but it did not rerun:
  - `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1`
  - `pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1`
  - `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1`
  - `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1`
  - `pwsh ./go.ps1`

QA interpretation:

- The feature itself looks well covered.
- The branch is not yet fully evidenced for final handoff until the repository gates are rerun on the remediated HEAD.

Residual risk:

- medium

## Recommended Next Checks Before Final Handoff

1. Run the full Mississippi quality pipeline on the current HEAD.
   - Minimum expectation: build, cleanup, unit tests, mutation tests, and final `go.ps1` confirmation.

2. Run a small real-storage negative smoke set in Crescent or an equivalent Azurite-backed harness.
   - Highest value cases:
   - duplicate-version write conflict
   - unreadable/corrupted blob read
   - `ValidateExists` startup against a missing container

3. Add or execute one worst-case payload smoke outside the current compressible matrix.
   - Use a low-compressibility payload.
   - Run at least one case with `Compression = Off` and one with `Compression = Gzip`.
   - The goal is not exhaustive performance benchmarking; it is proving there is no hidden size-path regression masked by the current synthetic payload.

4. Keep the repeated trust-slice loop as a release-candidate check for this branch.
   - The current `5/5` rerun result is good evidence.
   - Repeating that loop on a clean machine or CI agent would strengthen handoff confidence further.

## Bottom Line

From a QA perspective, this branch now has strong targeted test coverage for its most important correctness seams and a credible end-to-end proof for the main Blob snapshot story. The remaining risk is mostly about evidence breadth, not an obvious unfixed defect.

If the full repo gates pass on the current HEAD, I would consider the branch fit for handoff with one explicit note: real-infrastructure negative-path coverage is still thinner than the L0 seam coverage, so operational storage failures are the main residual risk area to monitor.