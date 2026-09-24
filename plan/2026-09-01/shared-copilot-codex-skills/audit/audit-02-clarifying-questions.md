# Clarifying questions

## (A) Resolved from repository evidence

1. The migration must preserve mandatory rules outside skills: issue #532,
   root `AGENTS.md`, shared policies, and the Clean Squad workflow agree.
2. Cursor mirrors are retired work, not migration candidates: #563 / PR #530.
3. The repository is pre-1.0, but persisted storage names are immutable:
   `GitVersion.yml` and backwards-compatibility/storage instructions agree.
4. The work should be split into foundation, pilot, migration, rationalization,
   consolidation, and final verification leaves: #533–#564 define those
   boundaries.

## (B) User decision

**Question:** Should the plan preserve all named Copilot surfaces, with full
behavioral conformance on CLI/Codex and discovery/integration smoke tests on
the other supported Copilot surfaces?

**Answer:** Yes. The user selected the recommended tiered-validation option.

## Remaining implementation decisions delegated to the contract gate

- Exact `.agents/skills` versus `.github/skills` cutover/adapter behavior.
- Host-by-host discovery, precedence, reload, and fallback semantics.
- Exact skill catalogue after activation/collision measurements.
- Whether documentation uses one routing skill, a small outcome set, or
  retained path-specific routing.
- Exact validator implementation details within the repository's existing
  PowerShell/Pester automation conventions.

## CoV

The only user-dependent compatibility decision was asked directly. All other
choices remain evidence-gated in #540, #543, and #555 rather than being
silently assumed.
