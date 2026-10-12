---
applyTo: 'docs/Docusaurus/docs/adr/[0-9][0-9][0-9][0-9]-*.md'
---

# Architecture Decision Records (MADR)

Governing thought: ADRs use the MADR 4.0.0 template, live in `docs/Docusaurus/docs/adr/` for Docusaurus publishing, and are immutable once accepted.

> Drift check: Review the MADR 4.0.0 specification at <https://adr.github.io/madr/> before modifying the [skill's MADR body template](../../.agents/skills/author-architecture-decision/assets/madr-body.md); check `docs/key-principles/architecture-decision-records.md` for foundational thinking.

## Rules (RFC 2119)

- Covered contributors **MUST** read the complete policy files for [ADR lifecycle](../../docs/Docusaurus/docs/adr/AGENTS.md#adr-lifecycle) and apply their clauses within this instruction's original path, content, and audience scope. Why: Relocation and optional skill selection do not narrow these obligations.
- For ADR drafting, revision, supersession, or verification, contributors **MUST** follow [author-architecture-decision](../../.agents/skills/author-architecture-decision/SKILL.md) with [its local binding](../../.agents/skills/author-architecture-decision/assets/madr-body.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.

## Scope and Audience

All contributors creating or modifying ADRs; the cs ADR Keeper agent is the primary author within the Clean Squad workflow.
