# 1) Initial draft

## Requirements and constraints

- Review the current uncommitted changes for increment 1 of the Tributary Blob storage provider.
- Focus on project wiring, public registration/options surface, startup validation scaffolding, and the new L0 tests.
- Report in chat only and in this file; do not modify implementation code while reviewing.
- Review every candidate commit file one-by-one and anchor findings to concrete file paths and line numbers.

## Base and head

- Current branch: `feature/tributary-blob-storage-provider`
- HEAD SHA: `c050e2a6280ede514bcf01c60b6dbb9ffd5a5db6`
- Base branch: `main`
- Base SHA: `c050e2a6280ede514bcf01c60b6dbb9ffd5a5db6`
- Note: `HEAD` currently matches `main`, so the review scope is the working tree diff against `HEAD`, not a branch diff.

## Initial plan

1. Compute the uncommitted changed-file list relative to `HEAD`, including untracked files.
2. Build a deterministic review checklist in alphabetical order.
3. Review each candidate commit file by reading the full file content and comparing tracked edits where applicable.
4. Produce a final report with coverage evidence, issue list, validation summary, and commit-readiness assessment.

## Assumptions and unknowns

- I treated `.thinking/**` as working notes, not candidate commit content, because the user asked for review of increment-01 implementation changes and requested the output be written under `.thinking/`.
- I treated `docs/Docusaurus/docs/adr/**` as candidate commit content because those files are untracked outside `.thinking/` and would broaden the commit if included.
- I did not run the full repository quality gate (`go.ps1`); validation here is targeted to the new project/test slice.

## Claim list

- Every candidate commit file outside `.thinking/**` was reviewed.
- Every finding below includes file path plus line numbers.
- The new Blob L0 tests were executed locally.

# 2) Verification questions

1. Is the changed-file list complete for the candidate commit, including untracked files?
2. Are we reviewing the right baseline, given that `HEAD` equals `main`?
3. Did the reviewed file set include all code, tests, docs, and solution wiring for the increment candidate?
4. Are the blocking findings supported by at least two signals each where appropriate?
5. Do the new tests cover the public registration surface that this increment introduces?
6. Does the public API surface expose behavior that is not actually implemented yet?
7. Would committing the current docs with this code still count as a small focused slice?

# 3) Independent answers

1. Yes. `git status --short` showed `M mississippi.slnx` plus untracked `docs/Docusaurus/docs/adr/`, `src/Tributary.Runtime.Storage.Blob/`, and `tests/Tributary.Runtime.Storage.Blob.L0Tests/`. I expanded those directories into concrete files and reviewed 26 candidate commit files outside `.thinking/**`.
2. Yes. `git rev-parse HEAD` and `git rev-parse main` both returned `c050e2a6280ede514bcf01c60b6dbb9ffd5a5db6`, so `main...HEAD` is empty. The meaningful review scope is the working tree relative to `HEAD`.
3. Yes. The reviewed set included the solution file, both new `.csproj` files, all new runtime/startup C# files, both new test C# files, both lock files, and all six new ADR files.
4. Yes.
   - Public-but-nonfunctional provider: the public registration entrypoints are exposed in `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProviderRegistrations.cs:28,67,97,116`, while every storage operation throws in `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProvider.cs:43,53,64,74,85`.
   - Inert public options: knobs are exposed in `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageOptions.cs:27,32,37,42`, but current runtime usage only appears in validator checks such as `src/Tributary.Runtime.Storage.Blob/Startup/SnapshotBlobStorageOptionsValidator.cs:34,39,44,54`.
   - Missing overload coverage: public overloads exist at `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProviderRegistrations.cs:67,97`, but the tests only exercise the parameterless and `IConfiguration` paths in `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderRegistrationsTests.cs:31,57`.
5. No. The two most review-sensitive public overloads, the connection-string overload and the `Action<SnapshotBlobStorageOptions>` overload, do not have dedicated tests.
6. Yes. The commit currently exposes a public registration surface and several public options for a provider that still throws on all read/write/delete/prune calls and does not yet consume several of the exposed options.
7. No, if `docs/Docusaurus/docs/adr/0001` through `0005` are included. Those ADRs define future naming, framing, serializer identity, compression, and write/list semantics rather than the increment-1 startup slice.

# 4) Final revised plan

1. Use the working-tree diff as the review baseline.
2. Treat `.thinking/**` as excluded working notes and review the 26 candidate commit files outside that tree.
3. Report findings by file, sorted by severity, with a separate coverage checklist proving every reviewed file reached `REVIEWED`.
4. Make the commit recommendation based on correctness of the public surface, startup validation quality, test coverage, and slice focus.

# 5) Review

## Coverage evidence

### Candidate commit changed-file count

- 26 files reviewed outside `.thinking/**`

### Candidate commit changed-file list

1. `docs/Docusaurus/docs/adr/0001-use-canonical-stream-identity-and-hashed-blob-naming-for-snapshot-blobs.md`
2. `docs/Docusaurus/docs/adr/0002-store-snapshots-in-a-versioned-self-describing-blob-frame.md`
3. `docs/Docusaurus/docs/adr/0003-persist-concrete-payload-serializer-identity-for-snapshot-bytes.md`
4. `docs/Docusaurus/docs/adr/0004-compress-only-snapshot-payload-bytes-and-verify-payload-integrity.md`
5. `docs/Docusaurus/docs/adr/0005-use-conditional-blob-creation-and-stream-local-maintenance-scans.md`
6. `docs/Docusaurus/docs/adr/0006-use-configurable-container-initialization-for-blob-snapshot-storage.md`
7. `mississippi.slnx`
8. `src/Tributary.Runtime.Storage.Blob/packages.lock.json`
9. `src/Tributary.Runtime.Storage.Blob/SnapshotBlobCompression.cs`
10. `src/Tributary.Runtime.Storage.Blob/SnapshotBlobContainerInitializationMode.cs`
11. `src/Tributary.Runtime.Storage.Blob/SnapshotBlobDefaults.cs`
12. `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageOptions.cs`
13. `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProvider.cs`
14. `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProviderLoggerExtensions.cs`
15. `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProviderRegistrations.cs`
16. `src/Tributary.Runtime.Storage.Blob/Startup/BlobContainerInitializer.cs`
17. `src/Tributary.Runtime.Storage.Blob/Startup/BlobContainerInitializerOperations.cs`
18. `src/Tributary.Runtime.Storage.Blob/Startup/IBlobContainerInitializerOperations.cs`
19. `src/Tributary.Runtime.Storage.Blob/Startup/SnapshotBlobStartupLoggerExtensions.cs`
20. `src/Tributary.Runtime.Storage.Blob/Startup/SnapshotBlobStorageOptionsValidator.cs`
21. `src/Tributary.Runtime.Storage.Blob/Startup/SnapshotPayloadSerializerResolver.cs`
22. `src/Tributary.Runtime.Storage.Blob/Tributary.Runtime.Storage.Blob.csproj`
23. `tests/Tributary.Runtime.Storage.Blob.L0Tests/packages.lock.json`
24. `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageOptionsTests.cs`
25. `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderRegistrationsTests.cs`
26. `tests/Tributary.Runtime.Storage.Blob.L0Tests/Tributary.Runtime.Storage.Blob.L0Tests.csproj`

### Review checklist

| File | Status | Review note |
| --- | --- | --- |
| `docs/Docusaurus/docs/adr/0001-use-canonical-stream-identity-and-hashed-blob-naming-for-snapshot-blobs.md` | REVIEWED | Future naming contract ADR; not increment-1 startup behavior. |
| `docs/Docusaurus/docs/adr/0002-store-snapshots-in-a-versioned-self-describing-blob-frame.md` | REVIEWED | Future persisted-frame ADR; not increment-1 startup behavior. |
| `docs/Docusaurus/docs/adr/0003-persist-concrete-payload-serializer-identity-for-snapshot-bytes.md` | REVIEWED | Future persisted serializer-identity ADR. |
| `docs/Docusaurus/docs/adr/0004-compress-only-snapshot-payload-bytes-and-verify-payload-integrity.md` | REVIEWED | Future compression/integrity ADR. |
| `docs/Docusaurus/docs/adr/0005-use-conditional-blob-creation-and-stream-local-maintenance-scans.md` | REVIEWED | Future write/list semantics ADR. |
| `docs/Docusaurus/docs/adr/0006-use-configurable-container-initialization-for-blob-snapshot-storage.md` | REVIEWED | Increment-1 startup behavior ADR; aligned with current scaffolding. |
| `mississippi.slnx` | REVIEWED | Adds the new runtime and L0 test projects to the solution. |
| `src/Tributary.Runtime.Storage.Blob/packages.lock.json` | REVIEWED | Expected dependency lock for new runtime project; no unexpected package sprawl. |
| `src/Tributary.Runtime.Storage.Blob/SnapshotBlobCompression.cs` | REVIEWED | Public compression enum added. |
| `src/Tributary.Runtime.Storage.Blob/SnapshotBlobContainerInitializationMode.cs` | REVIEWED | Public startup mode enum added. |
| `src/Tributary.Runtime.Storage.Blob/SnapshotBlobDefaults.cs` | REVIEWED | Public defaults/constants surface added. |
| `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageOptions.cs` | REVIEWED | Public options surface added. |
| `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProvider.cs` | REVIEWED | Provider shell exposes format but throws for all operations. |
| `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProviderLoggerExtensions.cs` | REVIEWED | Logging for the placeholder provider. |
| `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProviderRegistrations.cs` | REVIEWED | Public DI surface and startup initializer wiring. |
| `src/Tributary.Runtime.Storage.Blob/Startup/BlobContainerInitializer.cs` | REVIEWED | Hosted-service startup validation path. |
| `src/Tributary.Runtime.Storage.Blob/Startup/BlobContainerInitializerOperations.cs` | REVIEWED | Azure Blob SDK boundary for startup operations. |
| `src/Tributary.Runtime.Storage.Blob/Startup/IBlobContainerInitializerOperations.cs` | REVIEWED | Startup operations seam for testing. |
| `src/Tributary.Runtime.Storage.Blob/Startup/SnapshotBlobStartupLoggerExtensions.cs` | REVIEWED | Startup logging surface. |
| `src/Tributary.Runtime.Storage.Blob/Startup/SnapshotBlobStorageOptionsValidator.cs` | REVIEWED | Options validation rules. |
| `src/Tributary.Runtime.Storage.Blob/Startup/SnapshotPayloadSerializerResolver.cs` | REVIEWED | Startup serializer resolution logic. |
| `src/Tributary.Runtime.Storage.Blob/Tributary.Runtime.Storage.Blob.csproj` | REVIEWED | Project wiring and references are minimal and consistent. |
| `tests/Tributary.Runtime.Storage.Blob.L0Tests/packages.lock.json` | REVIEWED | Expected test dependency lock; no unexpected package sprawl. |
| `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageOptionsTests.cs` | REVIEWED | Covers defaults and validator failures. |
| `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderRegistrationsTests.cs` | REVIEWED | Covers base registration and startup validation paths, but not every public overload. |
| `tests/Tributary.Runtime.Storage.Blob.L0Tests/Tributary.Runtime.Storage.Blob.L0Tests.csproj` | REVIEWED | Test project wiring is minimal and consistent. |

## Validation summary

- `dotnet test .\tests\Tributary.Runtime.Storage.Blob.L0Tests\Tributary.Runtime.Storage.Blob.L0Tests.csproj -c Release`
  - Result: 11 tests passed, 0 failed, build succeeded.
- Editor/static check:
  - `mississippi.slnx`: no errors reported.
  - No build or analyzer breakage was surfaced during the targeted test run.
- Not run:
  - Full solution build/cleanup/test/mutation pipeline.

## Commit recommendation

- **Not acceptable to commit as-is** if the goal is a small focused increment-1 slice.
- The startup scaffolding itself is directionally sound, but the current commit publishes a public provider and public options surface that overpromise relative to the implemented behavior.

## Issue report

### `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProviderRegistrations.cs`

#### MAJOR | API/Compatibility

- **What’s wrong:** The increment exposes a fully public registration surface for a provider that still throws for every storage operation.
- **Evidence:** Public entrypoints exist at `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProviderRegistrations.cs:28,67,97,116`, but the registered provider throws `NotSupportedException` from every operation in `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProvider.cs:43,53,64,74,85`.
- **Fix:** Keep the registration/options surface internal until at least one supported end-to-end storage path exists, or reduce the slice to internal startup components that are not yet advertised as a usable provider.
- **How to verify:** Register the provider in a host and exercise `ISnapshotStorageProvider.ReadAsync` and `WriteAsync`; the provider should no longer fail with the increment placeholder exception.

#### MAJOR | Testing

- **What’s wrong:** Half of the newly introduced public registration overloads have no focused tests.
- **Evidence:** The connection-string overload and `Action<SnapshotBlobStorageOptions>` overload are public at `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageProviderRegistrations.cs:67,97`, but the current test file only has explicit coverage for the parameterless and `IConfiguration` paths at `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderRegistrationsTests.cs:31,57`.
- **Fix:** Add focused L0 tests for the connection-string overload and the `Action<SnapshotBlobStorageOptions>` overload, asserting both options binding and service registration behavior.
- **How to verify:** Add one test per overload and confirm the resolved `IOptions<SnapshotBlobStorageOptions>` values plus keyed Blob client/container resolution match expectations.

### `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageOptions.cs`

#### MAJOR | API/Compatibility

- **What’s wrong:** The public options surface publishes knobs whose behavior does not exist yet, which makes the increment misleading for consumers.
- **Evidence:** `BlobPrefix`, `Compression`, `ListPageSizeHint`, and `MaximumHeaderBytes` are public at `src/Tributary.Runtime.Storage.Blob/SnapshotBlobStorageOptions.cs:27,32,37,42`, but current runtime usage is limited to validator checks such as `src/Tributary.Runtime.Storage.Blob/Startup/SnapshotBlobStorageOptionsValidator.cs:34,39,44,54`; the provider/startup path never consumes those values functionally.
- **Fix:** Remove or internalize the inert options until the corresponding behavior ships, or keep the whole Blob registration/options surface internal for now.
- **How to verify:** Each public option should have at least one test proving it changes observable provider behavior rather than only passing validation.

### `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderRegistrationsTests.cs`

#### MINOR | Maintainability

- **What’s wrong:** The file uses nested test helper classes even though the repo guidance says test helpers should be top-level types.
- **Evidence:** `StubBlobContainerInitializerOperations` and `TestSerializationProvider` are nested inside the test class at `tests/Tributary.Runtime.Storage.Blob.L0Tests/SnapshotBlobStorageProviderRegistrationsTests.cs:252,277`.
- **Fix:** Move these helpers to top-level private/internal types in the same file or to dedicated helper files.
- **How to verify:** Re-run the existing L0 tests after moving the helpers; behavior should remain unchanged.

## Additional slice-focus note

- If the candidate commit includes `docs/Docusaurus/docs/adr/0001` through `0005`, it is no longer a tight increment-1 startup slice.
- Evidence:
  - `docs/Docusaurus/docs/adr/0001-use-canonical-stream-identity-and-hashed-blob-naming-for-snapshot-blobs.md:2`
  - `docs/Docusaurus/docs/adr/0002-store-snapshots-in-a-versioned-self-describing-blob-frame.md:2`
  - `docs/Docusaurus/docs/adr/0003-persist-concrete-payload-serializer-identity-for-snapshot-bytes.md:2`
  - `docs/Docusaurus/docs/adr/0004-compress-only-snapshot-payload-bytes-and-verify-payload-integrity.md:2`
  - `docs/Docusaurus/docs/adr/0005-use-conditional-blob-creation-and-stream-local-maintenance-scans.md:2`
- Recommendation: keep ADR-0006 with this increment if desired, but split ADR-0001 through `0005` into a separate architecture/docs commit.

## Totals

- Severity totals:
  - `MAJOR`: 3
  - `MINOR`: 1
  - `BLOCKER`: 0
  - `NIT`: 0
- Category totals:
  - `API/Compatibility`: 2
  - `Testing`: 1
  - `Maintainability`: 1

## Top risks + next actions

1. Do not commit the public registration/options surface while the provider still throws for all actual storage operations.
2. Either shrink the public surface to only implemented behavior or keep the Blob provider internal until increment 2+ supplies real functionality.
3. Add focused L0 tests for the connection-string and `Action<TOptions>` overloads before treating the public setup surface as stable enough to commit.
4. If docs are part of the commit, split ADR-0001 through `0005` out of the increment-1 implementation commit so the slice stays focused.