---
applyTo: '**'
---

# Build Issue Remediation Protocol

Governing thought: Fix each warning/error with the smallest safe edit in at most five focused attempts, deferring with context when blocked.

> Drift check: Open the build/cleanup/test scripts in `eng/src/agent-scripts/` before running them; scripts remain authoritative for switches and order.

## Rules (RFC 2119)

- Covered contributors **MUST** read and apply [the complete global policy](../agent-guidance/global-policy.md) under [root instruction loading](../../AGENTS.md#instruction-loading), retaining the audience and task conditions below. Why: These obligations remain mandatory independently of skill selection.
- For observed build, analyzer, or style failure diagnosis and remediation, contributors **MUST** follow [repair-build-failures](../../.agents/skills/repair-build-failures/SKILL.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.

## Scope and Audience

Agents fixing build/analyzer/style issues in Mississippi and Samples solutions.

## References

- [repository editor conventions](../../.editorconfig)
