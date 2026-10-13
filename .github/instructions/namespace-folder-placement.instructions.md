---
applyTo: '**/*.{cs,razor,css}'
---

# Namespace and Folder Placement

Governing thought: File location and namespace layout must be deterministic, vertically sliced, and repeatable so contributors can find behavior in the same place across framework, tests, and samples.

> Drift check: Review `Directory.Build.props`, `.github/instructions/naming.instructions.md`, and project role guidance in `.github/instructions/projects.instructions.md` before changing placement rules.

## Rules (RFC 2119)

- Covered contributors **MUST** read the complete policy files for [placement contracts](../../src/AGENTS.md#placement) and apply their clauses within this instruction's original path, content, and audience scope. Why: Relocation and optional skill selection do not narrow these obligations.

## Scope and Audience

Contributors moving or adding source files under `src/`, `tests/`, and `samples/` where namespace/folder consistency is required.
