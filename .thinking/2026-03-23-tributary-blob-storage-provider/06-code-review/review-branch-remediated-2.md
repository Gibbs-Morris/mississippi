# Crescent Trust-Slice Blocker Re-Review

Date: 2026-03-23
Branch: feature/tributary-blob-storage-provider
HEAD: 323199e07ed408b8cda50795f740a27d6421fbd5
Base: main @ c050e2a6280ede514bcf01c60b6dbb9ffd5a5db6

## 1) Initial draft

Requirements and constraints:

- Re-review only the remaining blocker from `.thinking/2026-03-23-tributary-blob-storage-provider/06-code-review/review-branch-remediated.md`.
- Focus only on the current Crescent Blob trust-slice implementation and recent stabilization changes.
- Determine whether the blocker is now resolved.
- Report only remaining material issues for that blocker.
- Write the validation summary to `.thinking/2026-03-23-tributary-blob-storage-provider/06-code-review/review-branch-remediated-2.md`.

Initial plan:

1. Compute the scoped changed-file list under `samples/Crescent/Crescent.L2Tests` from `main...HEAD`.
2. Build a deterministic review checklist in alphabetical order.
3. Review each scoped file one-by-one, reading the current file and the branch diff where relevant.
4. Validate the specific blocker by checking whether the current trust slice still depends on timeout-based polling for snapshot blob discovery.
5. Run the focused Crescent trust-slice test repeatedly.
6. Produce a final summary with coverage evidence and only any remaining material issue for this blocker.

Assumptions and unknowns:

- Scope is limited to the trust-slice project files under `samples/Crescent/Crescent.L2Tests`, because that is where the blocker existed and where the stabilization changes landed.
- Startup health waits for Aspire resources are not considered the blocker unless they are used as the trust-slice completion mechanism.

Claim list:

- Every scoped changed file was reviewed.
- The blocker determination is based on current file contents, current branch diffs, and repeated focused test execution.

## 2) Verification questions

1. Is the scoped changed-file list complete for the Crescent trust-slice implementation and its stabilization changes?
2. Was the review performed one file at a time in deterministic alphabetical order?
3. Does the current trust-slice test still contain timeout-based polling for aggregate readiness or blob discovery?
4. Does the current scenario still require open-ended blob listing or eventual-consistency polling to find the snapshot blob?
5. Did the stabilization changes introduce a deterministic completion signal or direct blob handle instead?
6. Are any remaining timeouts limited to environment startup and resource readiness rather than trust-slice completion?
7. Does repeated execution of the focused trust-slice test support the code-level conclusion?

## 3) Independent answers

1. Yes. `git diff --name-only --find-renames main...HEAD -- samples/Crescent/Crescent.L2Tests` returned 11 scoped files, all under the Crescent L2 test project.
2. Yes. The review checklist below is alphabetical, and each file reached `REVIEWED`.
3. No. The current trust-slice test reads state directly with `GetStateAsync` at [samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs](samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs#L59), persists the snapshot through `PersistSnapshotAsync` at [samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs](samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs#L65), restarts Orleans at [samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs](samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs#L87), and reads state directly again at [samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs](samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs#L92). The current file contains no `WaitForSnapshotBlobAsync`, `PeriodicTimer`, or `Task.Delay` loop.
4. No. The scenario computes the exact `SnapshotKey`, derives the exact blob path, and returns the exact `BlobClient` from `PersistSnapshotAsync` at [samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs](samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs#L162), [samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs](samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs#L180), [samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs](samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs#L198), and [samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs](samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs#L209). It checks for the exact blob with `ExistsAsync` at [samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs](samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs#L200) and [samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs](samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs#L213) instead of listing the container until something appears.
5. Yes. `PersistSnapshotAsync` is the stabilization seam that replaces discovery polling with a deterministic persisted-blob handle, then the test inspects and mutates that exact blob.
6. Yes. The remaining timeout usage in the scenario is limited to application start and Aspire resource health waits at [samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs](samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs#L36), [samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs](samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs#L107), [samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs](samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs#L113), and [samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs](samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs#L115). Those waits bound infrastructure startup; they are not the old trust-slice completion mechanism.
7. Yes. The focused command `1..5 | ForEach-Object { dotnet test .\samples\Crescent\Crescent.L2Tests\Crescent.L2Tests.csproj -c Release --filter FullyQualifiedName~BlobSnapshotTrustSliceTests --no-build }` completed successfully, which means all five iterations passed. The captured log includes successful summaries for later iterations, including `RUN 4` and `RUN 5`, each with `failed: 0, succeeded: 1`.

## 4) Final revised plan

1. Use the 11-file scoped list under `samples/Crescent/Crescent.L2Tests` as the complete review surface for this blocker.
2. Treat `BlobSnapshotTrustSliceScenario.cs` and `BlobSnapshotTrustSliceTests.cs` as the primary evidence files.
3. Use the remaining project files to confirm no hidden polling or alternate completion path was introduced elsewhere in the trust-slice project.
4. Close the blocker if the trust slice now uses direct state reads and a deterministic blob handle instead of timeout-based blob discovery.

## 5) Review

### Coverage evidence

Changed-file count in scope: 11

Changed files:

1. `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs`
2. `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs`
3. `samples/Crescent/Crescent.L2Tests/Crescent.L2Tests.csproj`
4. `samples/Crescent/Crescent.L2Tests/CrescentBlobCustomJsonSerializationProvider.cs`
5. `samples/Crescent/Crescent.L2Tests/LargeSnapshotAggregate.cs`
6. `samples/Crescent/Crescent.L2Tests/LargeSnapshotScenarioRegistrations.cs`
7. `samples/Crescent/Crescent.L2Tests/LargeSnapshotStored.cs`
8. `samples/Crescent/Crescent.L2Tests/LargeSnapshotStoredEventReducer.cs`
9. `samples/Crescent/Crescent.L2Tests/StoreLargeSnapshotCommand.cs`
10. `samples/Crescent/Crescent.L2Tests/StoreLargeSnapshotCommandHandler.cs`
11. `samples/Crescent/Crescent.L2Tests/packages.lock.json`

Review checklist:

- `REVIEWED` `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceScenario.cs`
- `REVIEWED` `samples/Crescent/Crescent.L2Tests/BlobSnapshotTrustSliceTests.cs`
- `REVIEWED` `samples/Crescent/Crescent.L2Tests/Crescent.L2Tests.csproj`
- `REVIEWED` `samples/Crescent/Crescent.L2Tests/CrescentBlobCustomJsonSerializationProvider.cs`
- `REVIEWED` `samples/Crescent/Crescent.L2Tests/LargeSnapshotAggregate.cs`
- `REVIEWED` `samples/Crescent/Crescent.L2Tests/LargeSnapshotScenarioRegistrations.cs`
- `REVIEWED` `samples/Crescent/Crescent.L2Tests/LargeSnapshotStored.cs`
- `REVIEWED` `samples/Crescent/Crescent.L2Tests/LargeSnapshotStoredEventReducer.cs`
- `REVIEWED` `samples/Crescent/Crescent.L2Tests/StoreLargeSnapshotCommand.cs`
- `REVIEWED` `samples/Crescent/Crescent.L2Tests/StoreLargeSnapshotCommandHandler.cs`
- `REVIEWED` `samples/Crescent/Crescent.L2Tests/packages.lock.json`

Per-file intent summary:

- `BlobSnapshotTrustSliceScenario.cs`: Starts the test environment and now exposes `PersistSnapshotAsync`, which resolves and returns the exact snapshot blob after the writer commits.
- `BlobSnapshotTrustSliceTests.cs`: Exercises large-snapshot write, exact blob inspection and overwrite, Orleans restart, and Blob-backed restart hydration without the old timeout/polling helper.
- `Crescent.L2Tests.csproj`: Adds the dependencies and project references needed for the trust-slice host and Blob inspection.
- `CrescentBlobCustomJsonSerializationProvider.cs`: Supplies the non-default serializer format that the trust slice verifies survives restart.
- `LargeSnapshotAggregate.cs`: Defines the snapshot state carried through the trust slice.
- `LargeSnapshotScenarioRegistrations.cs`: Registers the aggregate, command handler, reducer, and snapshot converter used by the trust slice.
- `LargeSnapshotStored.cs`: Defines the event emitted by the trust-slice command.
- `LargeSnapshotStoredEventReducer.cs`: Reduces the event into the aggregate state.
- `StoreLargeSnapshotCommand.cs`: Defines the command issued by the trust slice.
- `StoreLargeSnapshotCommandHandler.cs`: Emits the single large-snapshot event from the command.
- `packages.lock.json`: Captures the resolved package graph for the scoped trust-slice project.

### Issue report

No remaining material issues were found for this blocker.

The original blocker is resolved.

Reasoning:

- The current trust-slice test no longer discovers completion by waiting for any blob to appear in the container.
- The current scenario computes the exact blob name from the committed `SnapshotKey` and returns the exact `BlobClient` after the write path completes or after confirming that exact blob already exists.
- The remaining timeout usage is limited to test-environment startup and resource-health readiness, not blob discovery or trust-slice completion.
- The focused trust-slice test passed in a five-run loop on the current branch.

### Totals

- `BLOCKER`: 0
- `MAJOR`: 0
- `MINOR`: 0
- `NIT`: 0

By category:

- `Correctness`: 0
- `Security`: 0
- `Concurrency`: 0
- `Performance`: 0
- `Reliability`: 0
- `API/Compatibility`: 0
- `Testing`: 0
- `Observability`: 0
- `Maintainability`: 0
- `Style`: 0

### Top risks + next actions

Top risk:

- None material for this blocker. The previous timeout-based blob polling mechanism is not present in the current trust-slice implementation.

Next action:

- Close this blocker as resolved.
