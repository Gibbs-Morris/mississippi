---
applyTo: '**/*.cs'
---

# Orleans POCO Grains

Governing thought: Use Orleans 7+ POCO grains with `IGrainBase`, constructor injection, and extension methods—never inherit from `Grain`.

> Drift check: Review Orleans settings/packages in `Directory.Build.props` before editing grains.

## Rules (RFC 2119)

- Covered contributors **MUST** read the complete policy files for [Orleans contracts](../../src/AGENTS.md#orleans) and apply their clauses within this instruction's original path, content, and audience scope. Why: Relocation and optional skill selection do not narrow these obligations.

## Scope and Audience

Developers implementing Orleans grains and grain interfaces.
