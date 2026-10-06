---
applyTo: '**/*.cs'
---

# Domain Modeling with Mississippi Event Sourcing

Governing thought: Use consistent, attribute-driven domain modeling with immutable aggregates, typed handlers, and projection reducers following the Mississippi framework patterns.

> Drift check: Review `src/DomainModeling.Abstractions` and `src/Tributary.Abstractions` for base class signatures before implementing handlers or reducers.

## Rules (RFC 2119)

- Covered contributors **MUST** read the complete policy files for [event-sourced domain contracts](../../src/AGENTS.md#event-sourced-domain) and apply their clauses within this instruction's original path, content, and audience scope. Why: Relocation and optional skill selection do not narrow these obligations.
- For implementing or assessing an event-sourced application feature, contributors **MUST** follow [implement-event-sourced-feature](../../.agents/skills/implement-event-sourced-feature/SKILL.md) with [its local binding](../agent-guidance/event-sourced-feature-bindings.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.

### Domain Record Visibility

Apply [the domain record visibility contract](../../src/AGENTS.md#domain-record-visibility), including generated public signatures and exported-type discovery.

### Registration

Apply [the domain registration contract](../../src/AGENTS.md#event-sourced-domain), including its entrypoints and registration order.

## Scope and Audience

Developers implementing domain models using Mississippi event sourcing in samples or applications.
