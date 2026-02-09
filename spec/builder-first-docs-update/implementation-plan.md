# Implementation Plan

## Changes From Initial Draft
- Add middleware docs to the update list due to remaining ReservoirRegistrations links.
- Add explicit verification steps for link targets and builder-based examples.

## Detailed Checklist
1. Update Reservoir-related docs to builder-first registration:
	- docs/Docusaurus/docs/client-state-management/built-in-navigation.md
	- docs/Docusaurus/docs/client-state-management/built-in-lifecycle.md
	- docs/Docusaurus/docs/client-state-management/reservoir.md
	- docs/Docusaurus/docs/client-state-management/store.md
	- docs/Docusaurus/docs/client-state-management/reducers.md
	- docs/Docusaurus/docs/client-state-management/effects.md
	- docs/Docusaurus/docs/client-state-management/feature-state.md
	- docs/Docusaurus/docs/client-state-management/middleware.md
2. Update DevTools docs:
	- docs/Docusaurus/docs/client-state-management/devtools.md
3. Update sagas documentation:
	- docs/Docusaurus/docs/event-sourcing-sagas.md
4. Replace links to deleted ReservoirRegistrations with builder API links.
5. Verify no legacy IServiceCollection references remain in docs/Docusaurus/docs.
6. Update spec progress and summarize evidence.

## Test Plan
- Run markdownlint against docs:
  - npx markdownlint-cli2 "docs/Docusaurus/docs/**/*.md"

## Rollout Plan
- Docs-only change; no rollout actions.

## Risks and Mitigations
- Risk: missed page with legacy guidance. Mitigation: grep for IServiceCollection and ReservoirRegistrations references.
- Risk: link drift due to line number changes. Mitigation: spot-check key links during verification.
