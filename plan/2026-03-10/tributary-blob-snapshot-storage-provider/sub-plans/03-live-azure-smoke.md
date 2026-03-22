# Sub-Plan 03: Live Azure smoke

## Context
- Master plan: `/plan/2026-03-10/tributary-blob-snapshot-storage-provider/PLAN.md`
- This is sub-plan 03 of 4

## Dependencies
- Depends on: [02]
- PR 1 (plan commit) must be merged before execution

## Objective
Satisfy the issue's repeatable live Azure Blob requirement by adding an opt-in live-cloud verification path inside the Mississippi Blob provider L2 surface.

## Scope
- Live Azure configuration/fixture extensions in `tests/Tributary.Runtime.Storage.Blob.L2Tests/`
- Live-only test class(es) and skip/activation logic
- Documentation comments or test README notes needed to explain required Azure configuration for execution
- Any non-secret config binding code needed to resolve live Blob targets safely

## Deployability
- Feature gate: Live tests are opt-in and activate only when required Azure credentials/settings are supplied
- Safe to deploy: Runtime behavior is unchanged, and the live verification path safely skips by default when configuration is absent

## Implementation breakdown
1. Extend the L2 test surface with a dedicated live Azure fixture or environment path.
2. Bind required Blob account/container settings from configuration without introducing secrets into the repo.
3. Add live-cloud smoke tests for the core snapshot lifecycle with minimal cloud cost.
4. Ensure the live path skips cleanly when configuration is absent.
5. Validate that emulator-backed tests remain the default path.

## Testing strategy
- Keep Azurite L2 tests as the default path
- Run live tests only when configuration is intentionally supplied
- Verify skip behavior when live configuration is absent

## Acceptance criteria
- [ ] Builds with zero warnings
- [ ] All tests pass
- [ ] Deployable on its own (feature gated if incomplete)
- [ ] The repository contains a repeatable live Azure Blob smoke path
- [ ] Live tests never require secrets committed to the repo
- [ ] Live tests skip cleanly when Azure configuration is not present

## PR metadata
- Branch: `epic/tributary-blob-snapshot-storage-provider/03-live-azure-smoke`
- Title: `Add live Azure smoke coverage for Tributary Blob storage +semver: feature`
- Base: `main`

## Decomposition guardrails applied
- Feature gating via configuration presence keeps the live path safe and non-default
- Complete storage verification change: live-cloud verification lands as a coherent slice
- No partial runtime contract or storage-name changes cross sub-plan boundaries
