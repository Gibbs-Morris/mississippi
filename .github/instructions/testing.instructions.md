---
applyTo: '**'
---

# Testing, Coverage, and Mutation Strategy

Governing thought: Default to fast, deterministic L0 tests with strong coverage, using mutation testing proportionately as an additional quality signal.

> Drift check: Confirm script parameters in `eng/src/agent-scripts/` (or `./go.ps1`) before running; scripts are authoritative for order and options.

## Rules (RFC 2119)

- Covered contributors **MUST** read and apply [the complete global policy](../agent-guidance/global-policy.md) under [root instruction loading](../../AGENTS.md#instruction-loading), retaining the audience and task conditions below. Why: These obligations remain mandatory independently of skill selection.
- When selecting, running, or assessing change validation, contributors **MUST** follow [verify-change](../../.agents/skills/verify-change/SKILL.md) with [its local binding](../agent-guidance/verify-change-bindings.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.

## Scope and Audience

Applies to all test authors across Mississippi and Samples solutions, including mutation work and legacy test improvements.

## Change verification

Follow the linked verification skill and local binding; targeted or prerequisite checks do not replace completion gates.
