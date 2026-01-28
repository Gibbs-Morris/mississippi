---
applyTo: '**'
---

# Pull Request Review Guide

Governing thought: Deliver high-signal reviews that enforce small, single-responsibility PRs with proven tests and build results.

> Drift check: When referencing build/test commands, open the scripts under `eng/src/agent-scripts/`; scripts are authoritative.

## Rules (RFC 2119)

- Reviews **MUST** pause when diffs exceed ~600 changed lines; reviewers **SHOULD** request splits before continuing. Why: Keeps scope reviewable.
- Reviews **MUST** fail when L0 tests are missing for new code paths. Why: Tests are required for behavior changes.
- Reviewers **MUST** verify the author ran `pwsh ./go.ps1` or targeted quality scripts before approval. Why: Ensures gates passed.
- Pull requests **MUST** follow single-responsibility; mixed concerns **MUST** be split. Why: Prevents bundled refactors/features/cleanup.
- Feedback **SHOULD** be actionable (alternatives, slices) and **SHOULD** balance critique with reinforcement. Why: Helps authors improve quickly.

## Scope and Audience

PR reviewers in this repository.

## At-a-Glance Quick-Start

- Read description/links/screenshots; confirm single narrative and change type.
- Stop/split if scope or size is too large.
- Check build/test/mutation evidence (`./go.ps1` or equivalent).
- Inspect architecture boundaries, DI/logging patterns, and tests.
- Summarize must-fix items plus notable positives.

## Core Principles

- Small, focused PRs reduce risk.
- Tests and build evidence precede approval.
- Clear, direct feedback accelerates iteration.

## References

- Testing: `.github/instructions/testing.instructions.md`
- Logging/DI guardrails: `.github/instructions/logging-rules.instructions.md`, `.github/instructions/shared-policies.instructions.md`
- Documentation agent: `.github/agents/doc-writer.agent.md`
