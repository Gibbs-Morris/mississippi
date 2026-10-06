---
applyTo: '**'
---

# Build Rules and Quality Gates

Governing thought: Every change ships only after a clean build, cleanup, and tests with zero warnings; mutation testing provides an additional quality signal.

> Drift check: Verify commands in `eng/src/agent-scripts/` (or `./go.ps1`) before use; scripts are the source of truth for switches and order.

## Rules (RFC 2119)

- Covered contributors **MUST** read and apply [the complete global policy](../agent-guidance/global-policy.md) under [root instruction loading](../../AGENTS.md#instruction-loading), retaining the audience and task conditions below. Why: These obligations remain mandatory independently of skill selection.
- When selecting, running, or assessing change validation, contributors **MUST** follow [verify-change](../../.agents/skills/verify-change/SKILL.md) with [its local binding](../agent-guidance/verify-change-bindings.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.

## Scope and Audience

All contributors changing Mississippi or Samples solutions.
