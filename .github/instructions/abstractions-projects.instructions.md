---
applyTo: '**'
---

# Abstractions Projects

Governing thought: Split stable public contracts into `{Vendor}.{Area}[.{Feature}].Abstractions` projects so consumers take lightweight interfaces without implementations.

> Drift check: Open any referenced scripts/templates under `eng/src/` before use; scripts remain authoritative.

## Rules (RFC 2119)

- Covered contributors **MUST** read and apply [the complete global policy](../agent-guidance/global-policy.md) under [root instruction loading](../../AGENTS.md#instruction-loading), retaining the audience and task conditions below. Why: These obligations remain mandatory independently of skill selection.

## Scope and Audience

Applies whenever creating or updating libraries that expose contracts across assemblies/services.
