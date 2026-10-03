---
applyTo: '**'
---

# Mutation Testing Playbook (Mississippi)

Governing thought: Use mutation testing as a proportionate quality signal while prioritizing correct delivery, maintainability, and meaningful unit-test coverage.

> Drift check: Open `eng/src/agent-scripts/test-project-quality.ps1`, `mutation-test-mississippi-solution.ps1`, `summarize-mutation-survivors.ps1`, and `stryker-config.json` before use; their options and results describe tooling behavior, not mandatory repository score targets.

## Rules (RFC 2119)

- Explicit mutation work **MUST** follow the [run-mutation-testing](../../.agents/skills/run-mutation-testing/SKILL.md) skill. Why: The detailed procedure and evidence contract must be applied consistently.
- Mutation testing **MUST** remain an additional quality signal unless the caller explicitly makes it part of task acceptance. Why: The repository has no universal mutation completion gate.
- Agents **MUST NOT** impose a mutation-score threshold from tooling configuration or recommendations. Why: A configured score is not repository policy.
- Agents **MUST NOT** impose a maintain-or-raise requirement from tooling configuration or recommendations. Why: A configured trend is not repository policy.
- Agents **MUST** prioritize requested correctness, maintainability, zero-warning builds, and meaningful conventional tests. Why: These are the primary engineering outcomes.
- Mutation work **SHOULD** use a focused target and proportionate effort bound; an explicitly scoped and authorized broader run remains permitted. Why: Solution-wide execution is optional and can be expensive.
- Before an authorized mutation run, agents **MUST** restore the required tools. Why: Mutation evidence depends on valid preconditions.
- Before an authorized mutation run, agents **MUST** obtain the clean build required by the local binding. Why: Mutation evidence depends on a valid baseline.
- Mutation execution **MUST** remain stopped while build warnings or conventional-test failures invalidate its preflight. Why: A mutation score cannot validate an invalid baseline.
- Failed, skipped, interrupted, incomplete, or report-less mutation runs **MUST NOT** be reported as passing. Why: Optional execution does not justify overstating validation.
- Mutation score claims **MUST** identify the targets and revisions supported by valid reports. Why: Partial evidence cannot establish unreported coverage.
- Agents **MUST** distinguish mutation-tool or threshold failures from build/test failures and task acceptance. Why: Different failure classes require different decisions.
- Production code **MUST NOT** change solely to kill mutants unless evidence proves the survivor unkillable by appropriate tests and an authorized change under that exception includes its technical justification. Why: The justified exception must remain reviewable while mutation work preserves intended behavior.
- Agents **MUST** report execution status, scope, valid scores and report paths, significant gaps, and deferred work. Why: Quality signals need traceable evidence even when optional.
- Agents **SHOULD** inspect a valid existing report before starting another mutation run. Why: Reusing evidence can answer the question without another expensive execution.

## Scope and Audience

All agents planning, implementing, testing, or reviewing repository changes are
covered. The local mutation binding defines supported targets, commands, report
schemas, tool configuration, and any repository-specific exclusions; this policy
does not encode a sample-project ban.

## Mandatory route

For explicit mutation work, read [run-mutation-testing](../../.agents/skills/run-mutation-testing/SKILL.md)
first. If discovery is unavailable or applicability is unclear, read that
linked `SKILL.md` directly. Then read the [local mutation binding](../agent-guidance/mutation-testing-bindings.md)
before choosing a repository command; report any required guidance that
remains unavailable.

## References

- Canonical testing guidance: `.github/instructions/testing.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
