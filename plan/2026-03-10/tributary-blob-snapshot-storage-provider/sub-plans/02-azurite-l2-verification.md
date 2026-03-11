# Sub-Plan 02: Azurite L2 verification

## Context
- Master plan: `/plan/2026-03-10/tributary-blob-snapshot-storage-provider/PLAN.md`
- This is sub-plan 02 of 4

## Dependencies
- Depends on: [01]
- PR 1 (plan commit) must be merged before execution

## Objective
Add Mississippi-owned end-to-end Blob provider verification using a dedicated L2 project and companion Aspire AppHost backed by Azurite.

## Scope
- `tests/Tributary.Runtime.Storage.Blob.L2Tests/`
- `tests/Tributary.Runtime.Storage.Blob.L2Tests.AppHost/`
- AppHost Azure Storage/Azurite resource wiring
- End-to-end L2 fixtures and tests for snapshot lifecycle behavior
- Any solution wiring needed for the new test projects

## Deployability
- Feature gate: No user-visible behavior — test-only additive verification assets
- Safe to deploy: Production/runtime behavior is unchanged; this slice only increases verification coverage for the provider added in sub-plan 01

## Implementation breakdown
1. Create the Mississippi L2 test project and companion Aspire AppHost project.
2. Wire Azure Storage via Azurite using repo-standard Aspire patterns.
3. Register the Blob provider in the test host using keyed `BlobServiceClient` resolution.
4. Add L2 tests for write/read/list/prune and hosted initialization behavior.
5. Ensure the L2 project can run deterministically in local and CI emulator-backed environments.

## Testing strategy
- Run the new L2 project against Azurite through the AppHost
- Keep tests deterministic and isolated per repository guidance
- Retain targeted L0 coverage from sub-plan 01 as the fast inner loop

## Acceptance criteria
- [ ] Builds with zero warnings
- [ ] All tests pass
- [ ] Deployable on its own (feature gated if incomplete)
- [ ] A dedicated Mississippi L2 project and AppHost exist for Blob provider verification
- [ ] Azurite-backed tests prove end-to-end snapshot lifecycle behavior
- [ ] The test host uses repo-consistent Aspire and keyed Blob client wiring

## PR metadata
- Branch: `epic/tributary-blob-snapshot-storage-provider/02-azurite-l2-verification`
- Title: `Add Azurite L2 verification for Tributary Blob storage +semver: feature`
- Base: `main`

## Decomposition guardrails applied
- Complete storage changes: emulator-backed verification lands as a full test/AppHost pair in one slice
- No runtime contract fragmentation: provider runtime behavior from sub-plan 01 remains untouched
- No feature gate required because this is test-only additive work
