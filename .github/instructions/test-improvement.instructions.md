---
applyTo: '**'
---

# Legacy Test Improvement Loop

Governing thought: Improve meaningful unit-test coverage on legacy code, addressing mutation gaps proportionately and keeping production code untouched unless explicitly approved.

> Drift check: Confirm command flags in `eng/src/agent-scripts/test-project-quality.ps1` before use; script behavior is authoritative.

## Rules (RFC 2119)

- Work **MUST** stay under `tests/` unless explicit approval is given to change production code. Why: Assumes existing behavior is correct until tests prove otherwise.
- New warnings/errors **MUST** be fixed immediately; test code **MUST NOT** add suppressions or `NoWarn`. Why: Zero-warnings applies to tests.
- Changed code paths **MUST** aim for 100% conventional test coverage with no regressions. Why: Protects quality while improving legacy areas.
- Agents **MUST** follow the [mutation-testing policy](mutation-testing.instructions.md), including reporting significant historical gaps and deferring disproportionate remediation unless explicitly asked. Why: There is no mandatory repository mutation-score threshold.
- After the first clean build, agents **SHOULD** use `-NoBuild` for faster loops but **MUST** still run a build with `-warnaserror`. Why: Keeps iteration fast without skipping gates.
- Coverage and mutation gap tasks **SHOULD** be synced from existing reports with summarizer scripts, using `-SkipMutationRun` for mutation reports. Why: Keeps scratchpad deterministic without triggering unnecessary mutation runs.

- Agents **MUST** use [improve-legacy-tests](../../.agents/skills/improve-legacy-tests/SKILL.md) and the [local test-improvement binding](../agent-guidance/legacy-test-improvement-bindings.md) for legacy test improvement; if discovery is unavailable, read both linked files directly. Why: One procedure applies retained rules using current repository contracts.

## Scope and Audience

Agents improving tests on legacy/non-TDD areas.

## References

- Canonical testing guidance: `.github/instructions/testing.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
