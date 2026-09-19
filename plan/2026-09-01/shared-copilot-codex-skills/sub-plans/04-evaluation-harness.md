# Sub-Plan 04: Evaluation and validation harness

## Context

- Master plan: `../PLAN.md`
- Issue: #543
- This is sub-plan 04 of 24.

## Dependencies

- Depends on: 02, 03
- Plan approval/PR 1 is required before execution.

## Objective

Provide deterministic structural validation and bounded cross-agent activation
and output-quality evidence.

## Scope

- `eng/src/agent-scripts/RepositoryAutomation.psm1`
- `eng/src/agent-scripts/validate-agent-content.ps1`
- `eng/tests/agent-scripts/RepositoryAutomation.Tests.ps1`
- `.github/workflows/guidance-validation.yml`
- checked-in scenario/fixture and redacted JSON schemas

## Deployability

- Feature gate: validation-only; no active workflow changes.
- Safe to deploy: it can report the existing state before any redirect.

## Implementation breakdown

1. Add a parser-backed structural validator with stable diagnostic codes and
   `PASS`/`FAIL`/`BLOCKED` exit semantics.
2. Validate names, frontmatter, references, casing, Unicode collisions,
   handoffs, allowlists, cycles, capabilities, and stale roots.
3. Add explicit/implicit/paraphrase/incomplete/negative/collision/missing-tool/
   unsupported/adversarial scenario records.
4. Record host/model/version, fixture and contract hashes, outcomes, and
   redacted evidence; keep raw prompts/output out by default.
5. Wire offline checks to PR, merge group, push, and dispatch on Windows/Linux;
   bound live sampling to scheduled/explicit runs.

## Testing strategy

Use golden YAML/Markdown/agent fixtures for malformed and platform-specific
cases, process-level exit-code tests, and replayable scenario tests. Validate
case-sensitive checkout and generated JSON determinism.

## Acceptance criteria

- [ ] Command and Pester tests are runnable from the repository.
- [ ] Missing evidence/tools/authorization is `BLOCKED`, not a pass.
- [ ] Structural hard gates are offline and required in CI.
- [ ] Live evaluations are isolated, bounded, and least privilege.
- [ ] JSON artifacts are versioned, redacted, and reproducible.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/04-evaluation-harness`
- Title: `Build cross-agent skill validation harness +semver: skip`
- Base: `main`
