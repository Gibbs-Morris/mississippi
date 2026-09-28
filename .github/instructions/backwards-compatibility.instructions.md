---
applyTo: '**'
---

# Backwards Compatibility Policy

Governing thought: While the repository version is pre-1.0.0, breaking changes are freely permitted; agents must not add compatibility shims for patterns that only exist on the current branch.

> Drift check: Check `GitVersion.yml` (`next-version`) to confirm the current version bracket before applying compatibility rules.

## Rules (RFC 2119)

- Covered contributors **MUST** read and apply [the complete global policy](../agent-guidance/global-policy.md) under [root instruction loading](../../AGENTS.md#instruction-loading), retaining the audience and task conditions below. Why: These obligations remain mandatory independently of skill selection.

## Scope and Audience

All contributors and agents making code changes in this repository. This policy overrides any other instruction that implies backwards compatibility is required by default while the repo is pre-1.0.
