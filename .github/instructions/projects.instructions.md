---
applyTo: '**'
---

# Project File Management

Governing thought: Keep `.csproj` files minimal, CPM-driven, and free of duplicated settings from `Directory.Build.props`.

> Drift check: Open `Directory.Build.props` and `Directory.Packages.props` before editing a project file; they define defaults and versions.

## Rules (RFC 2119)

- Covered contributors **MUST** read and apply [the complete global policy](../agent-guidance/global-policy.md) under [root instruction loading](../../AGENTS.md#instruction-loading), retaining the audience and task conditions below. Why: These obligations remain mandatory independently of skill selection.
- When validating project changes, contributors **MUST** follow [verify-change](../../.agents/skills/verify-change/SKILL.md) with [its local binding](../agent-guidance/verify-change-bindings.md) and the [standalone script catalogue](../../eng/src/agent-scripts/README.md#script-catalogue). Why: Original build and final-build entrypoints retain their canonical carrier.

## Scope and Audience

Anyone creating or modifying `.csproj` files.

## References

- Project naming spec: `spec/renaming/target.md` (original authority; unresolved in the pinned tree).
