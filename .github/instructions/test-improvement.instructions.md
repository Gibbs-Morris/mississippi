---
applyTo: '**'
---

# Legacy Test Improvement Loop

Governing thought: Improve meaningful unit-test coverage on legacy code, addressing mutation gaps proportionately and keeping production code untouched unless explicitly approved.

> Drift check: Confirm command flags in `eng/src/agent-scripts/test-project-quality.ps1` before use; script behavior is authoritative.

## Rules (RFC 2119)

- Covered contributors **MUST** read and apply [the complete global policy](../agent-guidance/global-policy.md) under [root instruction loading](../../AGENTS.md#instruction-loading), retaining the audience and task conditions below. Why: These obligations remain mandatory independently of skill selection.
- For legacy test improvement, contributors **MUST** follow [improve-legacy-tests](../../.agents/skills/improve-legacy-tests/SKILL.md) with [its local binding](../agent-guidance/legacy-test-improvement-bindings.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.

## Scope and Audience

Agents improving tests on legacy/non-TDD areas.
