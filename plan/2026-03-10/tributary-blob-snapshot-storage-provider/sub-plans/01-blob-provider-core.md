# Sub-Plan 01: Blob provider core

## Context
- Master plan: `/plan/2026-03-10/tributary-blob-snapshot-storage-provider/PLAN.md`
- This is sub-plan 01 of 4

## Dependencies
- Depends on: none
- PR 1 (plan commit) must be merged before execution

## Objective
Deliver the additive Blob snapshot storage provider package with repository-standard registrations, deterministic blob pathing, provider facade behavior, and comprehensive L0 coverage.

## Scope
- `src/Tributary.Runtime.Storage.Blob/` new provider package
- Blob-specific defaults/options/registration types
- Blob repository, storage models, mappers, path builder, hosted initializer, and provider facade
- `tests/Tributary.Runtime.Storage.Blob.L0Tests/` new L0 test project
- Solution/project wiring required for the new source and test projects

## Deployability
- Feature gate: No user-visible behavior — additive provider package only, activated only when a host explicitly references and registers it
- Safe to deploy: Existing Cosmos and abstraction consumers remain unchanged; no host behavior changes occur unless the new provider is intentionally adopted

## Implementation breakdown
1. Create the Blob provider package using the Cosmos provider as the public-shape baseline.
2. Add module-owned Blob service defaults and `SnapshotStorageOptions`/registration overload parity.
3. Implement deterministic blob path construction and Blob repository read/write/list/prune behavior.
4. Implement the `ISnapshotStorageProvider` facade with parity logging/metrics behavior.
5. Add hosted initialization for any required container/bootstrap work.
6. Add L0 tests for registrations, provider facade behavior, mapping/pathing, repository semantics, conflict handling, and compression defaults.

## Testing strategy
- New Mississippi L0 project with focused tests for each provider layer
- Build new source and test projects with zero warnings
- Run targeted Blob-provider L0 tests during implementation

## Acceptance criteria
- [ ] Builds with zero warnings
- [ ] All tests pass
- [ ] Deployable on its own (feature gated if incomplete)
- [ ] `AddBlobSnapshotStorageProvider()` registration surface mirrors the Cosmos provider's consumer experience
- [ ] Blob pathing is deterministic and uses the required contract inputs
- [ ] Shared snapshot abstractions remain unchanged unless implementation evidence proves a gap
- [ ] L0 tests cover provider, registrations, and repository semantics

## PR metadata
- Branch: `epic/tributary-blob-snapshot-storage-provider/01-blob-provider-core`
- Title: `Add Tributary Blob snapshot provider core +semver: feature`
- Base: `main`

## Decomposition guardrails applied
- No partial contracts: the provider contract is fully implemented within this slice
- Complete storage changes: the Blob provider's core configuration and registration surface land together
- No feature gate required beyond explicit package adoption and service registration
