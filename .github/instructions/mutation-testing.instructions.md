---
applyTo: '**'
---

# Mutation Testing Playbook (Mississippi)

Governing thought: Use mutation testing as a proportionate quality signal while prioritizing correct delivery, maintainability, and meaningful unit-test coverage.

> Drift check: Open `eng/src/agent-scripts/test-project-quality.ps1`, `mutation-test-mississippi-solution.ps1`, `summarize-mutation-survivors.ps1`, and `stryker-config.json` before use; their options and results describe tooling behavior, not mandatory repository score targets.

## Rules (RFC 2119)

- Covered contributors **MUST** read and apply [the complete global policy](../agent-guidance/global-policy.md) under [root instruction loading](../../AGENTS.md#instruction-loading), retaining the audience and task conditions below. Why: These obligations remain mandatory independently of skill selection.
- For explicit mutation work, contributors **MUST** follow [run-mutation-testing](../../.agents/skills/run-mutation-testing/SKILL.md) with [its local binding](../agent-guidance/mutation-testing-bindings.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.

## Scope and Audience

All agents planning, implementing, testing, or reviewing repository changes are
covered. The local mutation binding defines supported targets, commands, report
schemas, tool configuration, and any repository-specific exclusions; this policy
does not encode a sample-project ban.

## Mandatory route

Follow the explicit mutation route above before choosing commands; its local binding supplies supported targets and report contracts.
