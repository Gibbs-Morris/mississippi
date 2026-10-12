---
applyTo: 'samples/**'
---

# Coding Discipline (Samples)

Governing thought: Sample applications follow strict architectural patterns—Redux on client, aggregates/projections on server, schema-first generation throughout. Deviations are refused.

> Drift check: Review `samples/Spring/` for reference patterns; `src/Reservoir/` for client state; `src/DomainModeling.Runtime/` for server patterns.

## Rules (RFC 2119)

- Covered contributors **MUST** read the complete policy files for [sample discipline](../../samples/AGENTS.md#sample-discipline), [event-sourced domain contracts](../../src/AGENTS.md#event-sourced-domain) and apply their clauses within this instruction's original path, content, and audience scope. Why: Relocation and optional skill selection do not narrow these obligations.
- For implementing or assessing an event-sourced application feature, contributors **MUST** follow [implement-event-sourced-feature](../../.agents/skills/implement-event-sourced-feature/SKILL.md) with [its local binding](../agent-guidance/event-sourced-feature-bindings.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.

## Scope and Audience

All contributors building sample applications with Mississippi. Samples serve as reference implementations—they demonstrate correct usage and must be exemplary.
