---
applyTo: '**'
---

# Shared Engineering Guardrails

Governing thought: Common rules for every instruction—zero warnings, centralized packages, consistent dependency injection, and LoggerExtensions logging—so other docs can stay concise.

> Drift check: When a rule references a repository script or config, open the referenced file under `eng/src/` or the repo root first; scripts/configs stay authoritative.

## Rules (RFC 2119)

- Covered contributors **MUST** read and apply [the complete global policy](../agent-guidance/global-policy.md) under [root instruction loading](../../AGENTS.md#instruction-loading), retaining the audience and task conditions below. Why: These obligations remain mandatory independently of skill selection.

## Scope and Audience

Applies to all contributors and all files in this repository; individual instruction files add domain-specific rules and should reference this file instead of duplicating these guardrails.
