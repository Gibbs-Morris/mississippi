---
applyTo: '**'
---

# Pull Request Review Guide

Governing thought: Deliver high-signal reviews that enforce small, single-responsibility PRs with proven tests and build results.

> Drift check: When referencing build/test commands, open the scripts under `eng/src/agent-scripts/`; scripts are authoritative.

## Rules (RFC 2119)

- Reviewers **MUST** verify [issue tracking and PR traceability](issue-tracking.instructions.md), including the documented plan, current issue status, and an appropriate issue reference in every PR description. Why: Review includes the requested outcome and its delivery record.
- Reviewers **MUST** apply [PR size and stacked delivery](pr-size-and-stacking.instructions.md): target 600 changed lines or fewer and assess larger coherent changes using the author's rationale and review path. Why: Reviewability requires judgment rather than automatic rejection by line count.
- Reviewers **MUST** assess a stacked PR against its immediate parent and verify the advancement gate before endorsing progression. Why: Ancestor changes are separate review units, and every layer needs current CI and resolved feedback.
- Reviews **MUST** fail when L0 tests are missing for new code paths. Why: Tests are required for behavior changes.
- Reviewers **MUST** verify the author ran `pwsh ./go.ps1` or targeted quality scripts before approval. Why: Ensures gates passed.
- Reviewers **MUST** apply the [mutation-testing policy](mutation-testing.instructions.md) when assessing mutation evidence and gaps. Why: Mutation scores are additional quality signals, not mandatory repository thresholds or ordinary completion criteria.
- Pull requests **MUST** follow single-responsibility; mixed concerns **MUST** be split. Why: Prevents bundled refactors/features/cleanup.
- Feedback **SHOULD** be actionable (alternatives, slices) and **SHOULD** balance critique with reinforcement. Why: Helps authors improve quickly.

## Scope and Audience

PR reviewers in this repository.

## At-a-Glance Quick-Start

- Read description/links/screenshots; confirm single narrative and change type.
- Request a logical split for mixed concerns or unmanageable review scope; assess justified size exceptions.
- Check build/test evidence (`./go.ps1` or equivalent), plus reported mutation results or an explicit not-run status.
- Inspect architecture boundaries, DI/logging patterns, and tests.
- Summarize must-fix items plus notable positives.

## Core Principles

- Small, focused PRs reduce risk.
- Tests and build evidence precede approval.
- Clear, direct feedback accelerates iteration.

## References

- Testing: `.github/instructions/testing.instructions.md`
- Logging/DI guardrails: `.github/instructions/logging-rules.instructions.md`, `.github/instructions/shared-policies.instructions.md`
- Post-push review polling: `.github/instructions/pr-review-polling.instructions.md`
- Documentation agent: `.github/agents/technical-writer.agent.md`
