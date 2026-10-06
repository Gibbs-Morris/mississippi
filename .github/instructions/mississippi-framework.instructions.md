---
applyTo: 'samples/**'
---

# Mississippi Framework Usage

Governing thought: Build applications using the Mississippi framework with source generation as the default, a clean four-project solution structure, Redux-style state management via Reservoir, and small componentized projections over brooks.

> Drift check: Review the framework source under `src/`, the Spring sample under `samples/Spring/`, and especially `Spring.Domain/` for domain patterns before implementing new features; established patterns are authoritative.

## Rules (RFC 2119)

- Covered contributors **MUST** read the complete policy files for [framework usage](../../samples/AGENTS.md#framework-usage), [event-sourced domain contracts](../../src/AGENTS.md#event-sourced-domain) and apply their clauses within this instruction's original path, content, and audience scope. Why: Relocation and optional skill selection do not narrow these obligations.
- For implementing or assessing an event-sourced application feature, contributors **MUST** follow [implement-event-sourced-feature](../../.agents/skills/implement-event-sourced-feature/SKILL.md) with [its local binding](../agent-guidance/event-sourced-feature-bindings.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.

## Scope and Audience

Applies to all contributors building sample applications or new features using the Mississippi framework. These rules ensure samples remain consistent, idiomatic, and serve as reference implementations for framework consumers.
