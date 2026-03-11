# Sub-Plan 04: Docs and wiring

## Context
- Master plan: `/plan/2026-03-10/tributary-blob-snapshot-storage-provider/PLAN.md`
- This is sub-plan 04 of 4

## Dependencies
- Depends on: [03]
- PR 1 (plan commit) must be merged before execution

## Objective
Publish the Blob provider documentation and verified wiring examples so users can adopt the provider with the same clarity currently offered by the Cosmos provider page.

## Scope
- `docs/Docusaurus/docs/tributary/storage-providers/blob.md`
- `docs/Docusaurus/docs/tributary/storage-providers/index.md`
- Verified framework-host wiring example source locations to cite in docs
- Verified sample-host/Aspire wiring example source locations to cite in docs
- Any minimal code/example assets needed solely to make the documented examples true and testable

## Deployability
- Feature gate: No user-visible runtime change beyond additive documentation and example validation assets
- Safe to deploy: Documentation and example wiring do not alter existing production behavior; provider adoption remains explicit and opt-in

## Implementation breakdown
1. Add the Blob provider page mirroring the Cosmos provider page shape.
2. Update the Tributary storage-provider overview page to list Blob support.
3. Document package names, registration, configuration options, Azurite-backed verification, and opt-in live Azure smoke prerequisites.
4. Include one verified framework-host example and one verified sample-host example, matching the user's parity choice.
5. Validate doc metadata, links, and Docusaurus conventions.

## Testing strategy
- Verify all documentation claims against the implemented provider/test assets
- Run any existing targeted doc validation/build path that applies to the touched docs
- Spot-check cited wiring examples against the actual source files

## Acceptance criteria
- [ ] Builds with zero warnings
- [ ] All tests pass
- [ ] Deployable on its own (feature gated if incomplete)
- [ ] A Blob provider page exists under Tributary storage-provider docs
- [ ] The overview page lists Blob alongside Cosmos
- [ ] Docs include both framework-host and sample-host wiring examples
- [ ] Docs explain Azurite-backed verification and the opt-in live Azure smoke path accurately

## PR metadata
- Branch: `epic/tributary-blob-snapshot-storage-provider/04-docs-and-wiring`
- Title: `Document Tributary Blob snapshot storage provider +semver: feature`
- Base: `main`

## Decomposition guardrails applied
- No partial runtime or storage contract work appears in the docs slice
- Documentation waits until the implementation and verification surfaces are stable
- No feature gate needed because adoption remains explicit and docs are additive
