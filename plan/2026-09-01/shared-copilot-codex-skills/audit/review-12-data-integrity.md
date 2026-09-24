# Review 12: Data Integrity and Storage Engineer

## Findings

- **High — Make evidence lineage immutable.** Store a commit-anchored sorted
  manifest with path, bytes, lines, SHA-256, scope, scanner version, command,
  and digest.
- **High — Give the migration matrix a canonical artifact.** Use a versioned
  machine-readable matrix with source/target digests, unit IDs, disposition,
  owner, supersession, and commit/PR evidence.
- **High — Define a cutover state machine.** Use
  `active-source -> shadow-replacement -> redirected -> retired`, file-set
  digests, inbound-reference checks, and tested forward/reverse rollback.
- **High — Protect skill and storage identities.** Define immutable skill IDs,
  rename/deprecation protocol, computed alias-derived storage names, and
  persisted storage identity checks.
- **High — Require orphan/reference closure.** Every discovered file maps to one
  matrix row; every destination exists; references resolve case-sensitively;
  retired paths have no inbound references except audit records.

## Strengths

The baselines match issue #532/#539, completed pre-work is preserved, and
introduce/redirect/retire appropriately delays destructive changes.

## CoV

The findings were checked against storage naming/backwards-compatibility rules,
current path references, and the draft's matrix, rollback, and metrics sections.
