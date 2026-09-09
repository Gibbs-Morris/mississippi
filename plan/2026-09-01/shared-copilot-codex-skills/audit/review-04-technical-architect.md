# Review 04: Technical Architect

## Findings

- **High — Canonical ownership needs a one-way table.** Distinguish invariant,
  workflow, agent-shell, adapter, deterministic-enforcement, and validation
  owners; adapters must not become normative copies.
- **High — Discovery and precedence are underspecified.** Define repository
  boundary, root-to-nearest nested `AGENTS.md`, additive versus nearest rules,
  skill search order, duplicate handling, instruction glob union, conflicts,
  and path normalization per host.
- **High — Portable skills must not bypass Clean Squad governance.** They may
  provide mechanics but cannot advance phase state, append the canonical ledger,
  select unauthorized agents, or publish governed artifacts.
- **High — Adapters need a contract.** Each must be a routing pointer, generated
  projection, or intentionally host-specific rule with fallback and a
  no-duplication validator.
- **Medium-high — Candidate skill boundaries collide.** `repair-build-failures`,
  `verify-change`, `review-change`, and `research-codebase` need explicit
  inclusion/exclusion prompts and arbitration before catalogue approval.

## Strengths

The destination split, staged rollback model, agent isolation preservation, and
pilot-before-expansion sequencing are architecturally sound.

## CoV

Findings were triangulated against current `AGENTS.md`, skill-builder and Rules
Manager contracts, Clean Squad `WORKFLOW.md`, and the issue decomposition.
